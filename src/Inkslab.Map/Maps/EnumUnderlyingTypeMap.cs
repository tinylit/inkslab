using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 枚举转换枚举或枚举与枚举基础类型之间的映射。
    /// </summary>
    public class EnumUnderlyingTypeMap : IMap
    {
        private static readonly List<Type> _enumTypes = new List<Type>(8)
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        private static readonly MethodInfo _toStringMtd = typeof(Enum).GetMethod(nameof(Enum.ToString), Type.EmptyTypes);

        private static readonly MethodInfo _concatMtd = MapConstants.StringType.GetMethod("Concat", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[3] { MapConstants.StringType, MapConstants.StringType, MapConstants.StringType }, null);

        /// <summary>
        /// 枚举因映射。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool IsMatch(Type sourceType, Type destinationType)
            => sourceType.IsEnum && destinationType.IsEnum
            || sourceType.IsEnum && _enumTypes.Contains(destinationType)
            || destinationType.IsEnum && _enumTypes.Contains(sourceType);

        /// <inheritdoc/>
        public Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            Type sourceType = sourceExpression.Type;

            if (sourceType.IsEnum && destinationType.IsEnum)
            {
                var destinationExpression = Variable(destinationType);

                var bodyExp = Call(MapConstants.TryParseMtd.MakeGenericMethod(destinationType), Call(sourceExpression, _toStringMtd), Constant(true, typeof(bool)), destinationExpression);

                return Block(destinationType, new ParameterExpression[1] { destinationExpression }, Condition(bodyExp, destinationExpression, Aw_ToSolve(sourceType, destinationType, sourceExpression)));
            }

            return Aw_ToSolve(sourceType, destinationType, sourceExpression);
        }

        private static Expression Aw_ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var sourceUnderlyingType = sourceType.IsEnum
                ? Enum.GetUnderlyingType(sourceType)
                : sourceType;

            var conversionUnderlyingType = conversionType.IsEnum
                ? Enum.GetUnderlyingType(conversionType)
                : sourceType;

            int indexOfSource = _enumTypes.IndexOf(sourceUnderlyingType);
            int indexOfDest = _enumTypes.IndexOf(conversionUnderlyingType);

            //? 目标类型值，大等于原类型值。
            if (indexOfSource <= indexOfDest)
            {
                int remainderSource = indexOfSource & 1;
                int remainderDest = indexOfDest & 1;

                if (remainderSource == remainderDest) //? 同是有符号或无符号。
                {
                    return Convert(sourceExpression, conversionType);
                }

                if (remainderSource == 0 && indexOfDest > indexOfSource + 1) //? 源是无符号，目标大一级；如：sbyte => short。
                {
                    return Convert(sourceExpression, conversionType);
                }

                //? 源有符号，目标类型为无符号。如:short, ushort
                return Block(IfThen(GreaterThan(sourceExpression, Constant(-1, sourceType)), ThrowError(sourceExpression, sourceType, sourceType, conversionType)), Convert(sourceExpression, conversionType));
            }

            ConstantExpression constantExpression = Type.GetTypeCode(sourceUnderlyingType) switch
            {
                TypeCode.SByte => Constant(sbyte.MaxValue, sourceUnderlyingType),
                TypeCode.Byte => Constant(byte.MaxValue, sourceUnderlyingType),
                TypeCode.Int16 => Constant(short.MaxValue, sourceUnderlyingType),
                TypeCode.UInt16 => Constant(ushort.MaxValue, sourceUnderlyingType),
                TypeCode.Int32 => Constant(int.MaxValue, sourceUnderlyingType),
                TypeCode.UInt32 => Constant(uint.MaxValue, sourceUnderlyingType),
                TypeCode.Int64 => Constant(long.MaxValue, sourceUnderlyingType),
                TypeCode.UInt64 => Constant(ulong.MaxValue, sourceUnderlyingType),
                _ => Constant(-1, sourceUnderlyingType),
            };

            return Block(IfThen(GreaterThan(Convert(sourceExpression, sourceUnderlyingType), constantExpression), ThrowError(Convert(sourceExpression, sourceUnderlyingType), sourceUnderlyingType, sourceType, conversionType)), Convert(sourceExpression, conversionType));
        }

        private static Expression ThrowError(Expression variable, Type sourceUnderlyingType, Type sourceType, Type conversionType)
        {
            return Throw(New(MapConstants.InvalidCastExceptionCtorOfString, Call(_concatMtd, Constant($"无法将类型({sourceType})的值"), Call(variable, sourceUnderlyingType.GetMethod("ToString", Type.EmptyTypes)!), Constant($"转换为类型({conversionType})!"))));
        }
    }
}

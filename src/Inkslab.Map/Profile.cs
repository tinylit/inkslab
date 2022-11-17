using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Map
{
    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class Profile : IProfile, IDisposable
    {
        private class Slot
        {
            public Slot()
            {
                Ignore = true;
            }

            public Slot(Expression valueExpression)
            {
                ValueExpression = valueExpression;
            }

            public bool Ignore { get; }

            public Expression ValueExpression { get; }
        }

        private class MapSlot
        {
            private readonly Type sourceType;
            private readonly List<Type> destinationTypes;
            private readonly Dictionary<string, Slot> memberExpressions = new Dictionary<string, Slot>();

            public MapSlot(Type sourceType, Type destinationType)
            {
                this.sourceType = sourceType;
                destinationTypes = new List<Type>(1) { destinationType };
            }

            public void Include(Type destinationType) => destinationTypes.Add(destinationType);

            public void Add(string memberName, Expression valueExpression) => memberExpressions[memberName] = new Slot(valueExpression);

            public void Ignore(string memberName) => memberExpressions[memberName] = new Slot();

            public bool TryGetSlot(string memberName, out Slot slot) => memberExpressions.TryGetValue(memberName, out slot);

            public bool IsMatch(Type sourceType, Type destinationType) => this.sourceType == sourceType && destinationTypes.Contains(destinationType);
        }

        private class MemberMappingExpression<TSource, TMember> : IMemberMappingExpression<TSource, TMember>
        {
            private readonly string memberName;
            private readonly MapSlot mapSlot;

            public MemberMappingExpression(string memberName, MapSlot mapSlot)
            {
                this.memberName = memberName;
                this.mapSlot = mapSlot;
            }

            public void Constant(TMember member) => mapSlot.Add(memberName, Expression.Constant(member, typeof(TMember)));

            public void ConvertUsing<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember, IValueConverter<TSourceMember, TMember> valueConverter)
            {
                if (sourceMember is null)
                {
                    throw new ArgumentNullException(nameof(sourceMember));
                }

                if (valueConverter is null)
                {
                    throw new ArgumentNullException(nameof(valueConverter));
                }

                Expression<Func<TSourceMember, TMember>> convertExp = x => valueConverter.Convert(x);

                mapSlot.Add(memberName, Lambda(Invoke(convertExp, sourceMember.Body), sourceMember.Parameters));
            }

            public void From(Expression<Func<TSource, TMember>> sourceMember)
            {
                if (sourceMember is null)
                {
                    throw new ArgumentNullException(nameof(sourceMember));
                }

                mapSlot.Add(memberName, sourceMember);
            }

            public void From(IValueResolver<TSource, TMember> valueResolver)
            {
                if (valueResolver is null)
                {
                    throw new ArgumentNullException(nameof(valueResolver));
                }

                From(x => valueResolver.Resolve(x));
            }

            public void Ignore() => mapSlot.Ignore(memberName);
        }

        private class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
        {
            private readonly MapSlot mapSlot;

            public MappingExpression(MapSlot mapSlot)
            {
                this.mapSlot = mapSlot;
            }

            public IMappingExpression<TSource, TDestination> Include<TAssignableToDestination>() where TAssignableToDestination : TDestination
            {
                mapSlot.Include(typeof(TAssignableToDestination));

                return this;
            }
            private static string NameOfAnalysis(Expression node)
            {
                return node switch
                {
                    MemberExpression member => member.Member.Name,
                    InvocationExpression invocation when invocation.Arguments.Count == 1 => NameOfAnalysis(invocation.Expression),
                    LambdaExpression lambda when lambda.Parameters.Count == 1 => NameOfAnalysis(lambda.Body),
                    _ => throw new NotSupportedException(),
                };
            }

            public IMappingExpressionBase<TSource, TDestination> Map<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberMappingExpression<TSource, TMember>> memberOptions)
            {
                if (destinationMember is null)
                {
                    throw new ArgumentNullException(nameof(destinationMember));
                }

                if (memberOptions is null)
                {
                    throw new ArgumentNullException(nameof(memberOptions));
                }

                string memberName = NameOfAnalysis(destinationMember);

                var options = new MemberMappingExpression<TSource, TMember>(memberName, mapSlot);

                memberOptions.Invoke(options);

                return this;
            }
        }

        private bool disposedValue;

        private readonly List<MapSlot> mapSlots = new List<MapSlot>();

        /// <summary>
        /// 解决映射关系。
        /// </summary>
        /// <param name="sourceExp">源。</param>
        /// <param name="destinationExpression">目标。</param>
        /// <param name="configuration">配置。</param>
        /// <returns>赋值表达式。</returns>
        /// <exception cref="InvalidCastException">类型不能被转换。</exception>
        protected virtual IEnumerable<BinaryExpression> ToSolve(ParameterExpression sourceExp, ParameterExpression destinationExpression, IMapConfiguration configuration)
        {
            bool flag = true;

            PropertyInfo[] propertyInfos = null;

            var validSlots = new List<MapSlot>();

            var sourceType = sourceExp.Type;
            var destinationType = destinationExpression.Type;

            foreach (var propertyInfo in destinationType.GetProperties())
            {
                if (!propertyInfo.CanWrite)
                {
                    continue;
                }

                if (flag) //? 匿名或元组等无可写属性的类型。
                {
                    flag = false;

                    do
                    {
                        for (int i = mapSlots.Count - 1; i >= 0; i--)
                        {
                            var mapSlot = mapSlots[i];

                            if (mapSlot.IsMatch(sourceType, destinationType))
                            {
                                validSlots.Add(mapSlot);
                            }
                        }

                        if (sourceType.BaseType is null)
                        {
                            break;
                        }

                        sourceType = sourceType.BaseType;

                    } while (sourceType != typeof(object));
                }

                foreach (var mapSlot in validSlots)
                {
                    if (mapSlot.TryGetSlot(propertyInfo.Name, out Slot slot))
                    {
                        if (slot.Ignore)
                        {
                            goto label_skip;
                        }

                        if (slot.ValueExpression is LambdaExpression lambda)
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), Invoke(lambda, sourceExp));
                        }
                        else
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), slot.ValueExpression);
                        }

                        goto label_skip;
                    }
                }

                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                propertyInfos ??= sourceType.GetProperties();

                foreach (var memberInfo in propertyInfos)
                {
                    if (memberInfo.CanRead && string.Equals(memberInfo.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (configuration.IsMatch(memberInfo.PropertyType, propertyInfo.PropertyType))
                        {
                            yield return Assign(Property(destinationExpression, propertyInfo), configuration.Map(Property(sourceExp, memberInfo), propertyInfo.PropertyType));
                        }
                        else
                        {
                            throw new InvalidCastException($"成员【{memberInfo.Name}】({memberInfo.PropertyType})无法转换为【{propertyInfo.Name}】({propertyInfo.PropertyType})类型!");
                        }

                        goto label_skip;
                    }
                }
label_skip:
                continue;
            }
        }

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源。/</param>
        /// <param name="destinationType">目标。</param>
        /// <returns>是否匹配。</returns>
        public bool IsMatch(Type sourceType, Type destinationType) => mapSlots.Exists(x => x.IsMatch(sourceType, destinationType));

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <param name="configuration">配置。</param>
        /// <returns>映射表达式。</returns>
        public Expression Map(Expression sourceExpression, Type destinationType, IMapConfiguration configuration)
        {
            var sourceType = sourceExpression.Type;

            if (sourceExpression is ParameterExpression parameterExpression)
            {
                return ToSolve(parameterExpression, destinationType, configuration);
            }

            var lambdaExp = ToSolve(Parameter(sourceType), destinationType, configuration);

            return Invoke(lambdaExp, sourceExpression);
        }

        /// <summary>
        /// 解决映射关系。
        /// </summary>
        /// <param name="sourceExpression">源。</param>
        /// <param name="destinationType">目标。</param>
        /// <param name="configuration">配置。</param>
        /// <returns>映射表达式。</returns>
        protected virtual LambdaExpression ToSolve(ParameterExpression sourceExpression, Type destinationType, IMapConfiguration configuration)
        {
            Type sourceType = sourceExpression.Type;

            var expressions = new List<Expression>();

            var destinationExpression = Variable(destinationType);

            var propertyInfos = sourceType.GetProperties();

            var nonParameterConstructorInfo = destinationType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);

            if (nonParameterConstructorInfo is null)
            {
                //? 无有效构造函数。
                bool invalidFlag = true;

                foreach (var constructorInfo in destinationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    //? 构造函数有效。
                    bool validFlag = true;

                    var parameterInfos = constructorInfo.GetParameters();

                    foreach (var parameterInfo in parameterInfos)
                    {
                        if (parameterInfo.IsOptional || parameterInfo.HasDefaultValue)
                        {
                            continue;
                        }

                        foreach (var propertyInfo in propertyInfos)
                        {
                            if (propertyInfo.CanRead && string.Equals(parameterInfo.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                if (IsMatch(parameterInfo.ParameterType, propertyInfo.PropertyType))
                                {
                                    continue;
                                }
                            }
                        }

                        validFlag = false;

                        break;
                    }

                    if (validFlag)
                    {
                        invalidFlag = false;

                        var arguments = new List<Expression>(parameterInfos.Length);

                        foreach (var parameterInfo in parameterInfos)
                        {
                            foreach (var propertyInfo in propertyInfos)
                            {
                                if (string.Equals(parameterInfo.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (configuration.IsMatch(parameterInfo.ParameterType, propertyInfo.PropertyType))
                                    {
                                        arguments.Add(configuration.Map(Property(sourceExpression, propertyInfo), parameterInfo.ParameterType));

                                        goto label_skip;
                                    }
                                }
                            }

                            arguments.Add(Constant(parameterInfo.DefaultValue, parameterInfo.ParameterType));

label_skip:
                            continue;
                        }

                        expressions.Add(Assign(destinationExpression, New(constructorInfo, arguments)));

                        break;
                    }
                }

                if (invalidFlag)
                {
                    throw new InvalidCastException($"无法从【{sourceType}】源映射到【{destinationType}】的任意构造函数！");
                }
            }
            else
            {
                expressions.Add(Assign(destinationExpression, New(nonParameterConstructorInfo)));
            }

            expressions.AddRange(ToSolve(sourceExpression, destinationExpression, configuration));

            return Lambda(Block(new ParameterExpression[1] { destinationExpression }, expressions), sourceExpression);
        }

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TSource">源。</typeparam>
        /// <typeparam name="TDestination">目标。</typeparam>
        /// <returns>映射关系表达式。</returns>
        public IMappingExpression<TSource, TDestination> Map<TSource, TDestination>()
            where TSource : class
            where TDestination : class
        {
            var mapSlot = new MapSlot(typeof(TSource), typeof(TDestination));

            mapSlots.Add(mapSlot);

            return new MappingExpression<TSource, TDestination>(mapSlot);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                mapSlots.Clear();

                disposedValue = true;
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }
    }
}

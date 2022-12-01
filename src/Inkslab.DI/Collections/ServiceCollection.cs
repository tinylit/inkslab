using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Inkslab.DI.Collections
{
    /// <summary>
    /// Default implementation of <see cref="IServiceCollection"/>.
    /// </summary>
    class ServiceCollection : IServiceCollection
    {
        private readonly Microsoft.Extensions.DependencyInjection.IServiceCollection _services;

        /// <inheritdoc />
        public ServiceCollection(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            _services = services;
        }

        /// <inheritdoc />
        public int Count => _services.Count;

        /// <inheritdoc />
        public bool IsReadOnly => _services.IsReadOnly;

        /// <inheritdoc />
        public ServiceDescriptor this[int index] { get => _services[index]; set => _services[index] = value; }

        /// <inheritdoc />
        public void Clear() => _services.Clear();

        /// <inheritdoc />
        public bool Contains(ServiceDescriptor item) => _services.Contains(item);

        /// <inheritdoc />
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _services.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(ServiceDescriptor item) => _services.Remove(item);

        /// <inheritdoc />
        public IEnumerator<ServiceDescriptor> GetEnumerator() => _services.GetEnumerator();

        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => _services.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(ServiceDescriptor item) => _services.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, ServiceDescriptor item) => _services.Insert(index, item);

        /// <inheritdoc />
        public void RemoveAt(int index) => _services.RemoveAt(index);
    }
}

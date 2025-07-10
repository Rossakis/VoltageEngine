using System;
using System.Collections.Generic;

namespace Nez.ECS
{
	/// <summary>
	/// Helps to register and create entities based on their type names.
	/// </summary>
	public static class EntityFactoryRegistry
    {
        private static readonly Dictionary<string, Func<Entity>> _factories = new();
        public static event Action<Entity> OnEntityCreated;

        public static void EntityCreated(Entity entity)
        {
	        OnEntityCreated?.Invoke(entity);
        }

		/// <summary>
		/// Register a factory for creating an entity of a specific type.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="factory"></param>
		public static void Register(string typeName, Func<Entity> factory)
        {
            _factories[typeName] = factory;
        }

		/// <summary>
		/// If the type is registered, create an instance of the entity.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public static bool TryCreate(string typeName, out Entity entity)
        {
            if (_factories.TryGetValue(typeName, out var factory))
            {
                entity = factory();

				return true;
            }
            entity = null;
            return false;
        }

        public static IEnumerable<string> GetRegisteredTypes() => _factories.Keys;
    }
}
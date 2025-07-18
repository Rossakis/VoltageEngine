using System;
using System.Collections.Generic;

namespace Nez.ECS;

/// <summary>
/// Helps to register and create entities based on their type names.
/// </summary>
public static class EntityFactoryRegistry
{
	private static readonly Dictionary<string, Func<Entity>> _factories = new();
	public static event Action<Entity> OnEntityCreated;

	public static void InvokeEntityCreated(Entity entity)
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
		try
		{
			entity = Create(typeName);
			return true;
		}
		catch
		{
			entity = null;
			return false;
		}
	}

	public static Entity Create(string typeName)
	{
		if (_factories.TryGetValue(typeName, out var factory))
		{
			return factory();
		}

		throw new InvalidOperationException(
			$"EntityFactoryRegistry: Entity type '{typeName}' is not registered in the factory. " +
			$"Did you forget to call EntityFactoryRegistry.Register(\"{typeName}\", ...)?");
	}

	public static IEnumerable<string> GetRegisteredTypes()
	{
		return _factories.Keys;
	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Nez.Data
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class EntityData
    {
        [HideAttributeInInspector]
        public List<ComponentDataEntry> ComponentDataList;

        [InspectableAttribute]
        public int NumberOfSerializedComponents => ComponentDataList?.Count ?? 0;

        public string EntityType;

		public EntityData()
		{
			ComponentDataList = new List<ComponentDataEntry>();
			EntityType = string.Empty;
		}

		public EntityData(Entity entity)
        {
            ComponentDataList = new List<ComponentDataEntry>();
            EntityType = entity.GetType().Name;
        }

        public EntityData(string entityType)
        {
            ComponentDataList = new List<ComponentDataEntry>();
            EntityType = entityType;
        }

        /// <summary>
        /// Creates a deep copy of this EntityData
        /// </summary>
        public EntityData Clone()
        {
            var clone = new EntityData
            {
                EntityType = EntityType,
                ComponentDataList = new List<ComponentDataEntry>()
            };

            // Deep clone the component data list
            if (ComponentDataList != null)
            {
                foreach (var entry in ComponentDataList)
                {
                    clone.ComponentDataList.Add(new ComponentDataEntry
                    {
                        ComponentTypeName = entry.ComponentTypeName,
                        ComponentName = entry.ComponentName,
                        DataTypeName = entry.DataTypeName,
                        Json = entry.Json // JSON is immutable string, safe to copy reference
                    });
                }
            }

            return clone;
        }
    }
}

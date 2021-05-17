using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Coop.Mod.Serializers
{
    [Serializable]
    internal class ItemRosterSerializer : ICustomSerializer
    {
        readonly List<byte[]> data = new List<byte[]>();

        public ItemRosterSerializer(ItemRoster roster)
        {
            foreach (ItemRosterElement item in roster)
            {
                // TaleWorlds BinaryWriter
                BinaryWriter writer = new BinaryWriter();
                // Have to get method info different due to the method being an explicit interface implementation
                MethodInfo serializeTo = typeof(ItemRosterElement)
                    .GetInterfaceMap(typeof(ISerializableObject))
                    .InterfaceMethods.First((methodInfo) => { return methodInfo.Name == "SerializeTo"; });
                serializeTo.Invoke(item, new object[] { writer });
                data.Add(writer.Data);
            }
        }

        public object Deserialize()
        {
            ItemRoster newRoster = new ItemRoster();

            List<ItemRosterElement> items = new List<ItemRosterElement>();
            foreach (byte[] element in data)
            {
                ItemRosterElement newItem = new ItemRosterElement();
                // TaleWorlds BinaryReader
                BinaryReader reader = new BinaryReader(element);
                // Have to get method info different due to the method being an explicit interface implementation
                MethodInfo deserializeFrom = typeof(ItemRosterElement)
                    .GetInterfaceMap(typeof(ISerializableObject))
                    .InterfaceMethods.First((methodInfo) => { return methodInfo.Name == "DeserializeFrom"; });
                deserializeFrom.Invoke(newItem, new object[] { reader });
                items.Add(newItem);
            }

            //  By default ItemRoster._data is null not an empty array, so in case items is empty,
            //      it must not change its value to an empty array
            if ( !items.IsEmpty() ) {
                typeof(ItemRoster)
                .GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(newRoster, items.ToArray() );
            }

            return newRoster;
            }
        }
}
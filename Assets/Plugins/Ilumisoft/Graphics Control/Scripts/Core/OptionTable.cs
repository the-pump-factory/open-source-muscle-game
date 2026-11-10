using System.Collections.Generic;
using System.Linq;

namespace Ilumisoft.GraphicsControl
{
    public class OptionTable<T>
    {
        public struct OptionEntry
        {
            public string Name;
            public T Value;

            public OptionEntry(string name, T value)
            {
                Name = name;
                Value = value;
            }
        }

        List<OptionEntry> entries = new();

        public void Add(string name, T value)
        {
            entries.Add(new OptionEntry(name, value));
        }

        public List<string> GetNames() => entries.Select(x=>x.Name).ToList();
        public List<T> GetValues() => entries.Select(x => x.Value).ToList();

        public T GetValue(int index) => entries[index].Value;

        public string GetName(int index) => entries[index].Name;
    }
}
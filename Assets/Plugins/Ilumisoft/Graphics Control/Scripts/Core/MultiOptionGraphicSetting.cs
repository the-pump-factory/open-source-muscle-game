using System;
using System.Collections.Generic;

namespace Ilumisoft.GraphicsControl
{
    public abstract class MultiOptionGraphicSetting<T> : GraphicSetting, IMultiOptionGraphicSetting
    {
        /// <summary>
        /// Option table holding all available options for the graphic setting (e.g. all available resolutions for the resolution setting)
        /// </summary>
        protected OptionTable<T> OptionTable = new();

        /// <summary>
        /// The index of the selected option (e.g. the selected resolution)
        /// </summary>
        int selectedIndex;

        /// <summary>
        /// Adds a new option to the option table
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected void AddOption(string name, T value)
        {
            OptionTable.Add(name, value);
        }

        /// <summary>
        /// Gets the names of all available options
        /// </summary>
        /// <returns></returns>
        public List<string> GetOptionNames()
        {
            return OptionTable.GetNames();
        }

        /// <summary>
        /// Sets the selected index
        /// </summary>
        /// <param name="index"></param>
        public void SetIndex(int index)
        {
            this.selectedIndex = index;
        }

        /// <summary>
        /// Gets the index of the selected option
        /// </summary>
        /// <returns></returns>
        public int GetIndex()
        {
            return selectedIndex;
        }

        /// <summary>
        /// Selects the given option. If the option is not available, the defaultIndex will be used to pick an option from the option table
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultIndex"></param>
        protected void SelectOption(T value, int defaultIndex = 0)
        {
            if (TryGetIndex(value, out var index))
            {
                SetIndex(index);
            }
            else
            {
                SetIndex(defaultIndex);
            }
        }

        /// <summary>
        /// Selects the option that fulfills the given predicate. If no option applies, the defaultIndex will be used to pick an option from the option table
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="defaultIndex"></param>
        protected void SelectOption(Predicate<T> predicate, int defaultIndex = 0)
        {
            if(TryGetIndex(predicate, out var index))
            {
                SetIndex(index);
            }
            else
            {
                SetIndex(defaultIndex);
            }
        }

        /// <summary>
        /// Tries to get the index of the given option
        /// </summary>
        /// <param name="option"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected bool TryGetIndex(T option, out int index)
        {
            return TryGetIndex(entry => entry.Equals(option), out index);
        }

        /// <summary>
        /// Tries to get the first index that fulfills the given predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        protected bool TryGetIndex(Predicate<T> predicate, out int index)
        {
            index = -1;

            var entries = OptionTable.GetValues();

            for (int i = 0; i < entries.Count; i++)
            {
                if (predicate(entries[i]))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the currently selected option
        /// </summary>
        /// <returns></returns>
        public T GetSelectedOption() => OptionTable.GetValue(selectedIndex);
    }
}
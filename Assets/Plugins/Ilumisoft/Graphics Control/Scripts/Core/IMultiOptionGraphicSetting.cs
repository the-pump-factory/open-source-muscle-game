using System.Collections.Generic;

namespace Ilumisoft.GraphicsControl
{
    public interface IMultiOptionGraphicSetting
    {
        List<string> GetOptionNames();
        int GetIndex();
        void SetIndex(int index);
    }
}
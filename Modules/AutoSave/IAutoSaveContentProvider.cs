using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Modules.AutoSave;
public interface IAutoSaveContentProvider<T>
{
    T Content { get; }
}

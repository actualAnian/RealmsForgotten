using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class TopScreenVM : ViewModel
    {
        private MBBindingList<CareerChoiceObjectVM> _choices = new();
        private string _groupName = "Test";
        [DataSourceProperty]
        public MBBindingList<CareerChoiceObjectVM> Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                if (value != _choices)
                {
                    _choices = value;
                    OnPropertyChangedWithValue(value, "Choices");
                }
            }
        }
        [DataSourceProperty]
        public string GroupName
        {
            get
            {
                return _groupName;
            }
            set
            {
                if (value != _groupName)
                {
                    _groupName = value;
                    OnPropertyChangedWithValue(value, "GroupName");
                }
            }
        }
    }
}

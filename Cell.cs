using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace SudokuSolver
{
    //Клетка
    public partial class Cell(int? value = null, bool variable = true) : ObservableObject
    {
        //Изменяемое ли значение
        [ObservableProperty]
        public partial bool Variable { get; set; } = variable;

        //Значение
        int? value = value;
        public int? Value
        {
            get => value;
            set
            {
                if (Variable && this.value != value)
                {
                    OnPropertyChanging(nameof(Value));
                    this.value = value == 0 ? null : value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        //Ошибочность значения
        [ObservableProperty]
        public partial bool IsError { get; set; } = false;

        //Выбрана ли ячейка
        [ObservableProperty]
        public partial bool IsSelected { get; set; } = false;

        //Толщина границ ячейки
        private Thickness borderThickness;
        public Thickness BorderThickness
        {
            get => borderThickness;
            set
            {
                borderThickness = value;
                OnPropertyChanged(nameof(BorderThickness));
            }
        }
    }
}

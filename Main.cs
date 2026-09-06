using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuSolver
{
    public partial class Main : ObservableObject
    {
        //Конструктор
        public Main()
        {
            NewGame();
        }

        //Игра
        [ObservableProperty]
        public partial Game Game { get; set; } = null!;

        //Высота и ширина новой игры
        [ObservableProperty]
        public partial int NewGameHeight { get; set; } = 3;

        [ObservableProperty]
        public partial int NewGameWidth { get; set; } = 3;

        //Процент заполнения новой игры
        [ObservableProperty]
        public partial int NewGameFillPercent { get; set; } = 50;

        //Создание новой игры
        [RelayCommand]
        public void NewGame()
        {
            Game = new Game(NewGameHeight, NewGameWidth, NewGameFillPercent);
        }

        //Ручная валидация игры
        [RelayCommand]
        public void ValidateGame()
        {
            Game.ValidateAllCells();
        }

        //Автоматическое решение игры
        [RelayCommand]
        public void SolveGame()
        {
            Game.Solve();
        }
    }
}

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
            Game = new Game(NewGameHeight, NewGameWidth);
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
        public async Task NewGame()
        {
            await Task.Run(() =>
            {
                Game = new Game(NewGameHeight, NewGameWidth);
                Game.FillGrid(NewGameFillPercent);
            });
        }

        //Очистить поле
        [RelayCommand]
        public async Task Clear()
        {
            await Task.Run(() =>
            {
                Game = new Game(Game.BlockHeight, Game.BlockWidth);
            });
        }

        //Ручная валидация игры
        [RelayCommand]
        public void ValidateGame()
        {
            Game.ValidateAllCells();
        }

        //Пошаговое автоматическое решение игры
        [RelayCommand]
        public void SolveGame()
        {
            Game.Solve();
        }

        //Полное решение
        [RelayCommand]
        public void FullSolveGame()
        {
            do
            {
                Game.Solve();
            } while (Game.IsSolving);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuSolver
{
    public partial class Main : ObservableObject
    {
        [ObservableProperty]
        public partial Game Game { get; set; } = null!;

        public Main()
        {
            NewGame();
        }

        [RelayCommand]
        public void NewGame()
        {
            Game = new Game(3, 3, 30);
        }

        [RelayCommand]
        public void ValidateGame()
        {
            Game.ValidateAllCells();
        }

        [RelayCommand]
        public void SolveGame()
        {
            Game.Solve();
        }
    }
}

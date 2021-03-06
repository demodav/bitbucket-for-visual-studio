﻿using System;
using GitClientVS.Contracts.Models;

namespace GitClientVS.Contracts.Interfaces.ViewModels
{
    public interface IConnectSectionViewModel : IViewModel, IViewModelWithErrorMessage, IInitializable, IDisposable
    {
        void ChangeActiveRepo();
        LocalRepo SelectedRepository { get; set; }
    }
}

﻿using GrinPlusPlus.Api;
using GrinPlusPlus.Resources;
using Prism.Navigation;
using Prism.Services;
using Prism.Services.Dialogs;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GrinPlusPlus.ViewModels
{
    public class InitPageViewModel : ViewModelBase
    {
        private string status = "";
        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        private double _progressBar = 0;
        public double ProgressBarr
        {
            get { return _progressBar; }
            set { SetProperty(ref _progressBar, value); }
        }

        private string _progressPercentage = "0";
        public string ProgressPercentage
        {
            get { return _progressPercentage; }
            set { SetProperty(ref _progressPercentage, value); }
        }

        public InitPageViewModel(INavigationService navigationService, IDataProvider dataProvider, IDialogService dialogService, IPageDialogService pageDialogService)
            : base(navigationService, dataProvider, dialogService, pageDialogService)
        {
            Status = AppResources.ResourceManager.GetString("InitializingNode");

            Preferences.Clear();
            SecureStorage.RemoveAll();

            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                if (Settings.Node.Status.Equals("Running"))
                {
                    ProgressBarr = 1;
                    ProgressPercentage = "100";
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await NavigationService.NavigateAsync("/SharedTransitionNavigationPage/LoginPage");
                    });
                    return false;
                }

                Status = Settings.Node.Status;

                ProgressBarr = Settings.Node.ProgressPercentage;
                ProgressPercentage = (ProgressBarr * 100).ToString("F");

                return true;
            });
        }
    }
}

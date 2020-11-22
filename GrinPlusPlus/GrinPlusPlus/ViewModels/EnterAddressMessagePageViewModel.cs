﻿using GrinPlusPlus.Api;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Prism.Services.Dialogs;
using System;
using ZXing.Mobile;

namespace GrinPlusPlus.ViewModels
{
    public class EnterAddressMessagePageViewModel : ViewModelBase
    {
        private double _amount;
        public double Amount
        {
            get { return _amount; }
            set { SetProperty(ref _amount, value); }
        }

        private double _fee;
        public double Fee
        {
            get { return _fee; }
            set { SetProperty(ref _fee, value); }
        }


        private string[] _inputs;
        public string[] Inputs
        {
            get { return _inputs; }
            set { SetProperty(ref _inputs, value); }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set { SetProperty(ref _address, value); }
        }

        private bool _isAddressValid;
        public bool IsAddressValid
        {
            get { return _isAddressValid; }
            set
            {
                SetProperty(ref _isAddressValid, value);
            }
        }

        private string _message = "";
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private MobileBarcodeScanningOptions _scannerOptions;
        public MobileBarcodeScanningOptions ScannerOptions
        {
            get { return _scannerOptions; }
            set { SetProperty(ref _scannerOptions, value); }
        }

        public DelegateCommand SendButtonClickedCommand => new DelegateCommand(SendButtonClicked);

        async void SendButtonClicked()
        {
            await NavigationService.NavigateAsync("SendingGrinsPage",
                new NavigationParameters
                {
                    { "address", Address },
                    { "message", string.IsNullOrEmpty(Message) ? "" : Message },
                    { "amount", Amount },
                    { "fee", Fee },
                    { "inputs", Inputs }
                }
            );
        }

        public DelegateCommand OnQRButtonClickedCommand => new DelegateCommand(OnQRButtonClicked);

        private async void OnQRButtonClicked()
        {
            await NavigationService.NavigateAsync(name: "QRScannerPage", parameters: null, useModalNavigation: true, animated: true);
        }

        public EnterAddressMessagePageViewModel(INavigationService navigationService, IDataProvider dataProvider, IDialogService dialogService, IPageDialogService pageDialogService)
            : base(navigationService, dataProvider, dialogService, pageDialogService)
        {
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            var navigationMode = parameters.GetNavigationMode();

            switch (navigationMode)
            {
                case Prism.Navigation.NavigationMode.New:
                    if (parameters.ContainsKey("amount"))
                    {
                        Amount = Double.Parse((string)parameters["amount"]);
                    }
                    if (parameters.ContainsKey("fee"))
                    {
                        Fee = Double.Parse((string)parameters["fee"]);
                    }
                    if (parameters.ContainsKey("inputs"))
                    {
                        Inputs = (string[])parameters["inputs"];
                    }
                    break;
                case Prism.Navigation.NavigationMode.Back:
                    if (parameters.ContainsKey("qr_scanner_result"))
                    {
                        Address = (string)parameters["qr_scanner_result"];
                    }
                    break;
            }
        }
    }
}

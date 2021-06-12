﻿using GrinPlusPlus.Api;
using GrinPlusPlus.Models;
using GrinPlusPlus.Resources;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace GrinPlusPlus.ViewModels
{
    public class WalletPageViewModel : ViewModelBase
    {
        private ObservableCollection<Transaction> _transactions = new ObservableCollection<Transaction>();
        public ObservableCollection<Transaction> Transactions
        {
            get { return _transactions; }
            set { SetProperty(ref _transactions, value); }
        }

        private Balance _balance;
        public Balance Balance
        {
            get { return _balance; }
            set { SetProperty(ref _balance, value); }
        }

        private bool _userCanSend = false;
        public bool UserCanSend
        {
            get { return _userCanSend; }
            set { SetProperty(ref _userCanSend, value); }
        }

        private Transaction _selectedTransaction;
        public Transaction SelectedTransaction
        {
            get { return _selectedTransaction; }
            set
            {
                SetProperty(ref _selectedTransaction, value);
            }
        }

        private string _torAddress = string.Empty;
        public string TorAddress
        {
            get { return _torAddress; }
            set { SetProperty(ref _torAddress, value); }
        }

        private string _wallet = "";
        public string Wallet
        {
            get { return _wallet; }
            set { SetProperty(ref _wallet, value); }
        }


        private string _statusIcon = "baseline_wifi_white_24.png";
        public string StatusIcon
        {
            get { return _statusIcon; }
            set { SetProperty(ref _statusIcon, value); }
        }

        private string _slatepackAddress = string.Empty;
        public string SlatepackAddress
        {
            get { return _slatepackAddress; }
            set { SetProperty(ref _slatepackAddress, value); }
        }

        public DelegateCommand OpenTransactionDetailsCommand => new DelegateCommand(OpenTransactionDetails);

        public DelegateCommand<object> CancelTransactionClickedCommand { get; private set; }

        public DelegateCommand SendButtonClickedCommand => new DelegateCommand(SendButtonClicked);

        public DelegateCommand ReceiveButtonClickedCommand => new DelegateCommand(ReceiveButtonClicked);

        public DelegateCommand FinalizeTransactionClickedCommand => new DelegateCommand(FinalizeTransactionClicked);

        public DelegateCommand ShareAddressCommand => new DelegateCommand(ShareAddress);

        private async void ShareAddress()
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Text = SlatepackAddress,
                Title = "grin"
            });
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (Settings.IsLoggedIn == false)
                {
                    return false;
                }

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await GetWalletBalance();
                });

                return true;
            });

            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (Settings.IsLoggedIn == false)
                {
                    return false;
                }

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadTransactions();
                });

                return true;
            });


            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                if (Settings.IsLoggedIn == false)
                {
                    return false;
                }

                if (Settings.Node.Status.Equals("Not Connected"))
                {
                    StatusIcon = "baseline_wifi_off_white_24.png";
                }
                else
                {
                    StatusIcon = "baseline_wifi_white_24.png";
                }

                return true;
            });

            Wallet = await SecureStorage.GetAsync("username");
            TorAddress = await SecureStorage.GetAsync("tor_address");
            SlatepackAddress = await SecureStorage.GetAsync("slatepack_address");
        }

        public WalletPageViewModel(INavigationService navigationService, IDataProvider dataProvider, IDialogService dialogService, IPageDialogService pageDialogService)
            : base(navigationService, dataProvider, dialogService, pageDialogService)
        {
            Balance = new Balance
            {
                Spendable = Preferences.Get("balance_spendable", 0.0),
                Locked = Preferences.Get("balance_locked", 0.0),
                Immature = Preferences.Get("balance_immature", 0.0),
                Unconfirmed = Preferences.Get("balance_unconfirmed", 0.0),
                Total = Preferences.Get("balance_total", 0.0)
            };

            UserCanSend = Balance.Spendable > 0;

            CancelTransactionClickedCommand = new DelegateCommand<object>(CancelTransactionClicked);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await GetWalletBalance();
            });

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadTransactions();
            });

            Device.StartTimer(TimeSpan.FromSeconds(30), () =>
            {
                if (Settings.IsLoggedIn == false)
                {
                    return false;
                }

                UpdateAvailability();

                return true;
            });
        }

        void UpdateAvailability()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {

                if (string.IsNullOrEmpty(TorAddress))
                {
                    Settings.Reachable = false;
                    return;
                }
                try
                {
                    Settings.Reachable = await DataProvider.CheckAddressAvailability(TorAddress, Settings.GrinChckAPIURL);
                }
                catch (Exception ex)
                {
                    Settings.Reachable = false;
                }
            });
        }

        private async Task GetWalletBalance()
        {
            try
            {
                var balance = await DataProvider.GetWalletBalance(await SecureStorage.GetAsync("token"));
                if (Balance.Total != balance.Total ||
                    Balance.Spendable != balance.Spendable ||
                    Balance.Immature != balance.Immature ||
                    Balance.Unconfirmed != balance.Unconfirmed ||
                    Balance.Locked != balance.Locked)
                {
                    Balance = balance;
                    UserCanSend = Balance.Spendable > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task LoadTransactions()
        {
            try
            {
                var transactions = await DataProvider.GetTransactions(await SecureStorage.GetAsync("token"),
                    new string[] { "SENDING_NOT_FINALIZED", "RECEIVING_IN_PROGRESS", "SENDING_FINALIZED" });

                if (Transactions.ToList().Count != transactions.Count)
                {
                    Transactions = new ObservableCollection<Transaction>(transactions.ToArray());
                }
                else
                {
                    foreach (var transaction in transactions)
                    {
                        try
                        {
                            var current = Transactions.First<Transaction>(t => t.Id.Equals(transaction.Id));
                            if (current != null)
                            {
                                if (!current.Status.Equals(transaction.Status))
                                {
                                    Transactions.Remove(current);
                                    Transactions.Add(transaction);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void SendButtonClicked()
        {
            if (UserCanSend)
            {
                await NavigationService.NavigateAsync("SetAmountPage", new NavigationParameters { { "spendable", Balance.Spendable / Math.Pow(10, 9) } });
            }
        }

        async void ReceiveButtonClicked()
        {
            await NavigationService.NavigateAsync("ReceiveTransactionPage");
        }


        async void FinalizeTransactionClicked()
        {
            await NavigationService.NavigateAsync("FinalizeTransactionPage");
        }

        async void CancelTransactionClicked(object id)
        {
            var cancelTransactionLabel = AppResources.ResourceManager.GetString("CancelTransaction");
            var cancelTransactionMessage = AppResources.ResourceManager.GetString("CancelTransactionQuestion");
            var cancelTransactionYes = AppResources.ResourceManager.GetString("Yes");
            var cancelTransactionNo = AppResources.ResourceManager.GetString("No");

            if (await PageDialogService.DisplayAlertAsync(cancelTransactionLabel, cancelTransactionMessage, cancelTransactionYes, cancelTransactionNo))
            {
                try
                {
                    await DataProvider.CancelTransaction(await SecureStorage.GetAsync("token"), (int)id);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        async void OpenTransactionDetails()
        {
            if (SelectedTransaction is null) return;
            await NavigationService.NavigateAsync("TransactionDetailsPage", new NavigationParameters { { "transaction", SelectedTransaction } });
            SelectedTransaction = null;
        }
    }
}

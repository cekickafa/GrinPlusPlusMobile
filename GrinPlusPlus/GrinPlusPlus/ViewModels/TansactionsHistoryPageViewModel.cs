﻿using GrinPlusPlus.Api;
using GrinPlusPlus.Models;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Essentials;

namespace GrinPlusPlus.ViewModels
{
    public class TansactionsHistoryPageViewModel : ViewModelBase
    {
        private ObservableCollection<TransactionGroup> _transactions;
        public ObservableCollection<TransactionGroup> Transactions
        {
            get { return _transactions; }
            set { SetProperty(ref _transactions, value); }
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

        public DelegateCommand OpenTransactionDetailsCommand => new DelegateCommand(OpenTransactionDetails);

        public TansactionsHistoryPageViewModel(INavigationService navigationService, IDataProvider dataProvider, IDialogService dialogService, IPageDialogService pageDialogService)
            : base(navigationService, dataProvider, dialogService, pageDialogService)
        {
            Transactions = new ObservableCollection<TransactionGroup>();

            Xamarin.Forms.Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var transactionsGroupedByDate = (await DataProvider.GetTransactions(
                                                               await SecureStorage.GetAsync("token"),
                                                               new string[] { "COINBASE", "SENT", "RECEIVED", "SENT_CANCELED", "RECEIVED_CANCELED" })
                                                        ).GroupBy(x => x.Date.ToString("dddd, dd MMMM yyyy"));
                        foreach (var group in transactionsGroupedByDate)
                        {
                            if (!Transactions.Any(t => t.Name.Equals(group.Key)))
                            {
                                Transactions.Add(new TransactionGroup(group.Key,
                                    transactionsGroupedByDate.First(g => g.Key.Equals(group.Key)).ToList()));
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
                return Settings.IsLoggedIn;
            });
        }

        async void OpenTransactionDetails()
        {
            if (SelectedTransaction is null) return;
            await NavigationService.NavigateAsync("TransactionDetailsPage", new NavigationParameters { { "transaction", SelectedTransaction } });
            SelectedTransaction = null;
        }
    }
}

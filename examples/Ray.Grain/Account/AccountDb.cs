﻿using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Ray.Core.Event;
using Ray.IGrains.Actors;
using Ray.Grain.Events;
using Ray.Core;

namespace Ray.Grain
{
    [Observer(DefaultObserverGroup.secondary, "db", typeof(Account))]
    public sealed class AccountDb : DbGrain<Account, long>, IAccountDb
    {
        public AccountDb(ILogger<AccountDb> logger) : base(logger)
        {
        }
        protected override bool ConcurrentHandle => true;
        public Task EventHandler(AmountTransferEvent evt, EventBase eventBase)
        {
            //Console.WriteLine($"更新数据库->用户转账,当前账户ID:{evt.StateId},目标账户ID:{evt.ToAccountId},转账金额:{evt.Amount},当前余额为:{evt.Balance}");
            return Task.CompletedTask;
        }
        public Task EventHandler(AmountAddEvent evt, EventBase eventBase)
        {
            //Console.WriteLine($"更新数据库->用户转账到账,用户ID:{evt.StateId},到账金额:{evt.Amount},当前余额为:{evt.Balance}");
            return Task.CompletedTask;
        }
    }
}
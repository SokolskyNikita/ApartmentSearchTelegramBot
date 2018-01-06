using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace FlatShareParser
{
    class TelegramHelper
    {
        TelegramBotClient Bot;

        public TelegramHelper(string ApiKey)
        {
            Bot = new Telegram.Bot.TelegramBotClient(ApiKey);
        }

        public void sendMessage(string message)
        {
            var t = Bot.SendTextMessageAsync(-223802849, message);
        }
        

    }
}

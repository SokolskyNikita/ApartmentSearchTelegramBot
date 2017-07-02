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

        public TelegramHelper()
        {
            Bot = new Telegram.Bot.TelegramBotClient("440800479:AAEJ8QZNBdWuz02Vc1Hr76mpY5LwMQRbZeA");
        }

        public void sendMessage(string message)
        {
            var t = Bot.SendTextMessageAsync(-231382622, message);
        }
        

    }
}

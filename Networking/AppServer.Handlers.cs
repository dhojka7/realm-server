using RotMG.Common;
using RotMG.Utils;
using System.Collections.Specialized;
using System.Net;
using System.Xml.Linq;

namespace RotMG.Networking
{
    public static partial class AppServer
    {
        private static byte[] CharList(HttpListenerContext context, NameValueCollection query)
        {
            XElement data = new XElement("Chars");

            bool accountInUse = false;
            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context)) ?? Database.GuestAccount();
                if (!(accountInUse = Database.IsAccountInUse(acc)))
                {
                    data.Add(new XAttribute("nextCharId", acc.NextCharId));
                    data.Add(new XAttribute("maxNumChars", acc.MaxNumChars));
                    data.Add(acc.Export());
                    data.Add(Database.GetNews(acc));
                    data.Add(new XElement("OwnedSkins", string.Join(",", acc.OwnedSkins)));
                    foreach (int charId in acc.AliveChars)
                    {
                        CharacterModel character = Database.LoadCharacter(acc, charId);
                        XElement export = character.Export();
                        export.Add(new XAttribute("id", charId));
                        data.Add(export);
                    }
                }
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return accountInUse ? WriteError("Account in use!") : Write(data.ToString());
        }

        private static byte[] Verify(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = WriteSuccess();
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] Register(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            string newUsername = query["newUsername"];
            string newPassword = query["newPassword"];

            if (!Database.IsValidUsername(newUsername))
                return WriteError("Invalid username.");

            if (!Database.IsValidPassword(newPassword))
                return WriteError("Invalid password.");

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                RegisterStatus status = Database.RegisterAccount(newUsername, newPassword, GetIPFromContext(context));
                if (status == RegisterStatus.Success)
                    data = WriteSuccess();
                else data = WriteError(status.ToString());
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] FameList(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            _listenEvent.Reset(); 
            Program.PushWork(() =>
            {
                data = Write(Database.GetLegends(query["timespan"]).ToString());
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();
            return data;
        }

        private static byte[] CharFame(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;
            int accId = int.Parse(query["accountId"]);
            int charId = int.Parse(query["charId"]);
            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                string legend = Database.GetLegend(accId, charId);
                data = string.IsNullOrWhiteSpace(legend) ? WriteError("Invalid character") : Write(legend);
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();
            return data;
        }

        private static byte[] CharDelete(HttpListenerContext context, NameValueCollection query)
        {
            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int charId = int.Parse(query["charId"]);

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.DeleteCharacter(acc, charId) ? WriteSuccess() : WriteError("Issue deleting character");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] AccountPurchaseCharSlot(HttpListenerContext context, NameValueCollection query)
        {

            byte[] data = null;

            string username = query["username"];
            string password = query["password"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.BuyCharSlot(acc) ? WriteSuccess() : WriteError("Not enough fame");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] AccountPurchaseSkin(HttpListenerContext context, NameValueCollection query)
        {

            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            int skinType = int.Parse(query["skinType"]);

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.BuySkin(acc, skinType) ? WriteSuccess() : WriteError("Could not buy skin");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }

        private static byte[] AccountChangePassword(HttpListenerContext context, NameValueCollection query)
        {

            byte[] data = null;

            string username = query["username"];
            string password = query["password"];
            string newPassword = query["newPassword"];

            _listenEvent.Reset();
            Program.PushWork(() =>
            {
                AccountModel acc = Database.Verify(username, password, GetIPFromContext(context));
                if (acc == null)
                    data = WriteError("Invalid account.");
                else if (Database.IsAccountInUse(acc))
                    data = WriteError("Account in use!");
                else
                    data = Database.ChangePassword(acc, newPassword) ? WriteSuccess() : WriteError("Could not change password");
            }, () => _listenEvent.Set());
            _listenEvent.WaitOne();

            return data;
        }
    }
}

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Matrix.Xmpp;
using Microsoft.AspNet.SignalR;

using Matrix.Xmpp.Client;

namespace WebClient.Common
{
    public class MatrixHub : Hub
    {
        private static readonly Dictionary<string, XmppClient> XmppClients = new Dictionary<string, XmppClient>();

        public override Task OnDisconnected()
        {
            Debug.WriteLine("SignalR Disconnect: " + Context.ConnectionId);

            if (XmppClients.ContainsKey(Context.ConnectionId))
            {
                var xmppClient = XmppClients[Context.ConnectionId];


                xmppClient.OnReceiveXml -= xmppClient_OnReceiveXml;
                xmppClient.OnSendXml -= xmppClient_OnSendXml;


                xmppClient.OnPresence -= xmppClient_OnPresence;
                xmppClient.OnMessage -= xmppClient_OnMessage;
                xmppClient.OnIq -= xmppClient_OnIq;

                xmppClient.OnRosterStart -= xmppClient_OnRosterStart;
                xmppClient.OnRosterItem -= xmppClient_OnRosterItem;
                xmppClient.OnRosterEnd -= xmppClient_OnRosterEnd;

                xmppClient.OnLogin -= xmppClient_OnLogin;

                xmppClient.OnClose -= xmppClient_OnClose;
                xmppClient.OnBeforeSendPresence -= xmppClient_OnBeforeSendPresence;
                xmppClient.OnBeforeSasl -= xmppClient_OnBeforeSasl;

                XmppClients.Remove(Context.ConnectionId);
            }

            return Clients.All.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task OnConnected()
        {
            Debug.WriteLine("SignalR Connect: " + Context.ConnectionId);

            if (XmppClients.Count == 0)
                SetLicense();

            if (!XmppClients.ContainsKey(Context.ConnectionId))
            {
                var xmppClient = new XmppClient();


                xmppClient.OnReceiveXml += xmppClient_OnReceiveXml;
                xmppClient.OnSendXml += xmppClient_OnSendXml;

                
                xmppClient.OnPresence   += xmppClient_OnPresence;
                xmppClient.OnMessage    += xmppClient_OnMessage;
                xmppClient.OnIq         += xmppClient_OnIq;

                xmppClient.OnRosterStart += xmppClient_OnRosterStart;
                xmppClient.OnRosterItem += xmppClient_OnRosterItem;
                xmppClient.OnRosterEnd += xmppClient_OnRosterEnd;
                
                xmppClient.OnLogin += xmppClient_OnLogin;

                xmppClient.OnClose += xmppClient_OnClose;
                xmppClient.OnBeforeSendPresence += xmppClient_OnBeforeSendPresence;
                xmppClient.OnBeforeSasl += xmppClient_OnBeforeSasl;

                XmppClients.Add(Context.ConnectionId, xmppClient);
            }
            
            return Clients.All.joined(Context.ConnectionId, DateTime.Now.ToString());
        }
        public override Task OnReconnected()
        {
            return Clients.All.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }

        void xmppClient_OnIq(object sender, IqEventArgs e)
        {
            DisplayEvent("OnIq");
        }

        void xmppClient_OnMessage(object sender, MessageEventArgs e)
        {
            DisplayEvent("OnMessage");

            Clients.Client(Context.ConnectionId).onMessage(
                   new Messages{From = e.Message.From, Body = e.Message.Body}
                   );
               
        }

        void xmppClient_OnBeforeSasl(object sender, Matrix.Xmpp.Sasl.SaslEventArgs e)
        {
            DisplayEvent("OnBeforeSasl");
        }

        void xmppClient_OnBeforeSendPresence(object sender, PresenceEventArgs e)
        {
            DisplayEvent("OnBeforeSendPresence");
        }

        void xmppClient_OnClose(object sender, Matrix.EventArgs e)
        {
            DisplayEvent("OnClose");
        }

        void xmppClient_OnLogin(object sender, Matrix.EventArgs e)
        {
            DisplayEvent("OnLogin");
        }

        void xmppClient_OnRosterEnd(object sender, Matrix.EventArgs e)
        {
            DisplayEvent("OnRosterEnd");
        }

        void xmppClient_OnRosterStart(object sender, Matrix.EventArgs e)
        {
            DisplayEvent("OnRosterStart");
        }
      
        void xmppClient_OnPresence(object sender, PresenceEventArgs e)
        {
            DisplayEvent("OnPresence");

            string uniqueId = Matrix.Util.Hash.Sha1HashHex(e.Presence.From.Bare);
            Clients.Client(Context.ConnectionId).onPresence(
                    uniqueId,
                    e.Presence.From.Bare,
                    e.Presence.Type == PresenceType.unavailable ? "Offline" : ShowToString(e.Presence.Show),
                    e.Presence.Status ?? ""
                );
        }

        void xmppClient_OnRosterItem(object sender, Matrix.Xmpp.Roster.RosterEventArgs e)
        {
            DisplayEvent("OnRosterItem");

            string uniqueId = Matrix.Util.Hash.Sha1HashHex(e.RosterItem.Jid.Bare);
            Clients.Client(Context.ConnectionId).onRosterItem(uniqueId, e.RosterItem.Jid.Bare, e.RosterItem.Name ?? "");
        }

        void xmppClient_OnSendXml(object sender, Matrix.TextEventArgs e)
        {
            var text = HttpUtility.HtmlEncode(String.Format("Send: {0}", e.Text));
           
            Clients.Client(Context.ConnectionId).sendXml(text);
        }

        void xmppClient_OnReceiveXml(object sender, Matrix.TextEventArgs e)
        {
            var text = HttpUtility.HtmlEncode(String.Format("Recv: {0}", e.Text));
           
            Clients.Client(Context.ConnectionId).receiveXml(text);
        }
        
        void DisplayEvent(string evt)
        {
            Clients.Client(Context.ConnectionId).displayEvent(evt);
        }

        public void Connect(String username, String password, String xmppDomain)
        {
           
            XmppClient xmppClient = XmppClients[Context.ConnectionId];
            xmppClient.Username = username;
            xmppClient.Password = password;
            xmppClient.XmppDomain = xmppDomain;

            xmppClient.Open();
        }

        public void Close()
        {
            XmppClient xmppClient = XmppClients[Context.ConnectionId];
            xmppClient.Close();
        }
        
        private string ShowToString(Show show)
        {
            switch (show)
            {
                case Show.NONE:
                    return "Online";
                case Show.away:
                    return "Away";
                case Show.xa:
                    return "Extended away";
                case Show.dnd:
                    return "Do not disturb";
                case Show.chat:
                    return "I want to chat";
            }

            return "Offline";
        }

        public void SendChatMessage(string to, string text)    {
        XmppClient xmppClient = XmppClients[Context.ConnectionId];
        xmppClient.Send(new Message {
            Type = MessageType.chat,
                    To = to,
                    Body = text
                });
        }
     

        private void SetLicense()
        {
            // get a trial license online at:
            // http://www.ag-software.net/matrix-xmpp-sdk/request-demo-license/
            const string LIC = @"set your license key here";
            Matrix.License.LicenseManager.SetLicense(LIC);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Podio.API.Model;
using Cometd.Client;
using Cometd.Client.Transport;
using Cometd.Bayeux;
using Cometd.Bayeux.Client;
using Cometd.Common;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace PodioDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    class Listener : IMessageListener
    {
        public void onMessage(IClientSessionChannel channel, IMessage message)
        {
            // Handle the message
            Dictionary<string, object> msg = extract_msg(message);
            Console.WriteLine("------------Listener------------" + msg["event"] + "\t" + msg["type"] + "\t" + msg["id"]);

            string push_note = msg["event"] + "\t" + msg["type"] + " " + msg["id"];
            pop_alert(push_note);
        }

        private Dictionary<string, object> extract_msg(IMessage msg)
        {

            Dictionary<string, object> dic_data = (Dictionary<string, object>)msg["data"];
            Dictionary<string, object> dic_data_detail = (Dictionary<string, object>)dic_data["data"];

            Dictionary<string, object> dic = new Dictionary<string, object>();
            string evnt = dic_data_detail["event"].ToString();
            if (evnt == "viewing" || evnt == "typing")
            {
                dic.Add("event", dic_data_detail["event"]);
                dic.Add("type", "what to see who it is?");
                dic.Add("id", "support lite!");
            }
            else
            {
                Dictionary<string, object> dic_data_ref = (Dictionary<string, object>)dic_data_detail["data"];
                Dictionary<string, object> dic_type_id = (Dictionary<string, object>)dic_data_ref["data_ref"];

                dic.Add("event", dic_data_detail["event"]);
                dic.Add("type", dic_type_id["type"]);
                dic.Add("id", dic_type_id["id"]);
            }            
            return dic;
        }

        private void pop_alert(string push_note)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new System.Action(() =>
            {
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                PopAlertWindow pop = new PopAlertWindow(push_note);
                int window_num = System.Windows.Application.Current.Windows.Count - 1;
                pop.Left = workingArea.Width - 300;
                pop.Top = workingArea.Height - window_num * 100;
                pop.Show();
            }));
        }

    }

    class Extention : IExtension
    {
        private IDictionary<String, Object> _msg_ext = new Dictionary<String, Object>();
        public Extention(IDictionary<string,object> ext)
        {
            _msg_ext = ext;
        }
        public bool rcv(Cometd.Bayeux.Client.IClientSession session, Cometd.Bayeux.IMutableMessage message) 
        {
            //Console.WriteLine("MSG from Extention rcv" + message);
            return true;
        }
        public bool rcvMeta(Cometd.Bayeux.Client.IClientSession session, Cometd.Bayeux.IMutableMessage message)
        {
            //Console.WriteLine("MSG from Extention rcvMeta" + message);
            return true;
        }
        public bool send(Cometd.Bayeux.Client.IClientSession session, Cometd.Bayeux.IMutableMessage message)
        {
            //Console.WriteLine("MSG from Extention send" + message);
            return true;
        }
        public bool sendMeta(Cometd.Bayeux.Client.IClientSession session, Cometd.Bayeux.IMutableMessage message)
        {
            message.getExt(true);
            message.Ext.Add("private_pub_signature", _msg_ext["private_pub_signature"]);
            message.Ext.Add("private_pub_timestamp", _msg_ext["private_pub_timestamp"]);
            //Console.WriteLine("MSG from Extention sendMeta" + message);
            return true;
        }
    }

    public partial class MainWindow : Window
    {

        /* The "id" podio gives your application when you generate a key (usually the name you give your Application)
         * the Client Secret Guid podio generates when you create an API Key.
         * your Podio login email, the password for your Podio account */
        Podio.API.Client client; 
        
        private int currentSpaceId = 1434205; //initiate it with podio test space

        public MainWindow()
        {
            client = (Podio.API.Client)System.Windows.Application.Current.Properties["podio_client"];
            if (client != null)
            {
                InitializeComponent();
                this.Left = 0; 

                //minimize icon
                System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
                ni.Icon = new System.Drawing.Icon("podio.ico");
                ni.Visible = true;
                ni.DoubleClick +=
                    delegate(object sender, EventArgs args)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    };

                start_subscribe(); 
            }
            else
            {
                MessageBox.Show("Unable to get the PODIO client.", "Error", MessageBoxButton.OK);
                App.Current.Shutdown(-1);
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void start_subscribe()
        {
            String url = "https://push.podio.com/faye";
            Dictionary<string, object> push_info = new Dictionary<string,object>();
            bool IsSubscribe = false;

            IEnumerable<Organization> orgs = client.OrganisationService.GetOrganizations();
            foreach (Organization org in orgs)
            {
                foreach (Space space in client.OrganisationService.GetSpacesOnOrganization((int)org.OrgId))
                {
                    int spaceId = (int)space.SpaceId;

                    if (spaceId == 1434205) //test: only listen to podio test
                    {
                        foreach (StreamObject stream in client.StreamService.GetSpaceStream(spaceId, 1, 0)) //10 will make high load 
                        {
                            switch (stream.Type)
                            {
                                case "action":
                                    push_info = get_push(stream.Id.ToString(), "action");
                                    IsSubscribe = true;
                                    break;
                                case "file":
                                    push_info = get_push(stream.Id.ToString(), "file");
                                    IsSubscribe = true;
                                    break;
                                case "item":
                                    push_info = get_push(stream.Id.ToString(), "item");
                                    IsSubscribe = true;
                                    break;
                                case "status":
                                    push_info = get_push(stream.Id.ToString(), "status");
                                    IsSubscribe = true;
                                    break;
                                default:
                                    IsSubscribe = false;
                                    break;
                            }
                            if (IsSubscribe)
                            {
                                BayeuxClient fayeclient = new BayeuxClient(url, new List<ClientTransport>() { new LongPollingTransport(null) });
                                fayeclient.handshake();
                                fayeclient.waitFor(1000, new List<BayeuxClient.State>() { BayeuxClient.State.CONNECTED });

                                IDictionary<String, Object> Auth = new Dictionary<String, Object>();
                                Auth.Add("private_pub_signature", push_info["signature"]);
                                Auth.Add("private_pub_timestamp", push_info["timestamp"]);
                                Extention CometDext = new Extention(Auth);
                                fayeclient.addExtension(CometDext);
                                IClientSessionChannel channel = fayeclient.getChannel(push_info["channel"].ToString());
                                channel.subscribe(new Listener());

                                IsSubscribe = false;
                            }
                        }
                    }
                }
            }

        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            UserShow.Visibility = System.Windows.Visibility.Hidden;
            SpaceShow.Visibility = System.Windows.Visibility.Hidden;
            HomeShow.Visibility = System.Windows.Visibility.Visible;

            FlowDocument FlowDoc = new FlowDocument();
            Paragraph para = new Paragraph();

            try
            {
                IEnumerable<Organization> orgs = client.OrganisationService.GetOrganizations();

                foreach (Organization org in orgs)
                {
                    para.Inlines.Add(new Run(org.Name));
                    foreach (Space space in client.OrganisationService.GetSpacesOnOrganization((int)org.OrgId)) //client.OrganisationService.GetSpacesOnOrganization((int)orgs.First().OrgId //OrgId is nullable
                    {
                        Hyperlink link = new Hyperlink();
                        link.IsEnabled = true;
                        link.Inlines.Add(space.Name);
                        link.NavigateUri = new Uri(space.Url);
                        link.CommandParameter = space.SpaceId;
                        link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Workspace_Click);

                        para.Inlines.Add("\r\n\t");
                        para.Inlines.Add(link);
                    }
                    para.Inlines.Add("\r\n");
                }

                FlowDoc.Blocks.Add(para);
                RichHomeDisplay.Document = FlowDoc;
            }
            catch (Exception ex)
            {
            }

        }

        private void User_Click(object sender, RoutedEventArgs e)
        {
            SpaceShow.Visibility = System.Windows.Visibility.Hidden;
            HomeShow.Visibility = System.Windows.Visibility.Hidden;
            UserShow.Visibility = System.Windows.Visibility.Visible;

            string requestUrl = Podio.API.Constants.PODIOAPI_BASEURL + "/user/status";
            Podio.API.Utils.PodioRestHelper.PodioResponse response = Podio.API.Utils.PodioRestHelper.Request(requestUrl, client.AuthInfo.AccessToken);
            string contents = response.Data;
            Dictionary<string, object> dic_all = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents);
            string str_profile = dic_all["profile"].ToString();
            Dictionary<string, object> dic_profile = JsonConvert.DeserializeObject<Dictionary<string, object>>(str_profile);

            if (UserInfo.Children.Count < 1)
            {
                TextBlock text = new TextBlock();
                text.TextWrapping = TextWrapping.WrapWithOverflow;
                text.FontFamily = new System.Windows.Media.FontFamily("Arial");
                text.FontSize = 19;

                Hyperlink link = new Hyperlink();
                link.IsEnabled = true;
                link.Inlines.Add(dic_profile["name"].ToString());
                link.NavigateUri = new Uri(dic_profile["link"].ToString());
                link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(link_RequestNavigate);

                text.Inlines.Add("User Name: ");
                text.Inlines.Add(link);
                text.Inlines.Add("\n\nUser Id: " + dic_profile["user_id"].ToString() + "\n\nLast Seen On: " + dic_profile["last_seen_on"].ToString());

                string[] emails = dic_profile["mail"].ToString().Split('\"');
                int email_num = (emails.Count() - 1) / 2;
                if (email_num > 0.5)
                {
                    text.Inlines.Add("\n\nEmail: ");
                    for (int i = 0; i < email_num; i++)
                    {
                        text.Inlines.Add(emails[i * 2 + 1] + " ");
                    }
                }
                UserInfo.Children.Add(text);
            }
        }

        private void Workspace_Click(object sender, RoutedEventArgs e)
        {
            UserShow.Visibility = System.Windows.Visibility.Hidden;
            HomeShow.Visibility = System.Windows.Visibility.Hidden;
            SpaceShow.Visibility = System.Windows.Visibility.Visible;

            int spaceId;
            if (sender.GetType().Name == "Hyperlink")
            {
                Hyperlink lnk = (Hyperlink)sender;
                spaceId = (int)lnk.CommandParameter;
                currentSpaceId = spaceId;
            }
            else //"Button"
            {
                spaceId = currentSpaceId;
            }

            FlowDocument FlowDoc = new FlowDocument();
            Paragraph para = new Paragraph();
            foreach (StreamObject stream in client.StreamService.GetSpaceStream(spaceId, 10, 0))//729327 is the id of OneFix Support; 1434205 is PODIO Test
            {
                Hyperlink link = new Hyperlink();
                link.IsEnabled = true;
                link.Inlines.Add(stream.Title);
                link.NavigateUri = new Uri(stream.Link);
                link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(link_RequestNavigate);
                TextBlock blk_link = new TextBlock(); //use textblock to wrap the content if too long
                blk_link.TextWrapping = TextWrapping.Wrap;
                blk_link.Inlines.Add(link);

                StackPanel comm_panel = new StackPanel();
                comm_panel.Margin = new Thickness(10,4,0,0);

                foreach (Comment comm in stream.Comments){
                    FlowDocument fld = new FlowDocument();
                    Paragraph par = new Paragraph();
                    par.Inlines.Add(comm.CreatedBy.Name + ": " + comm.Value);
                    fld.Blocks.Add(par);
                    RichTextBox comm_richbox = new RichTextBox(fld);
                    comm_panel.Children.Add(comm_richbox);
                }

                TextBox comm_text = new TextBox();
                comm_text.Height = 60; 
                comm_text.SpellCheck.IsEnabled = true; 
                comm_text.AcceptsReturn = true; 
                comm_text.IsReadOnly = false;
                comm_text.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible; 
                comm_text.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                comm_panel.Children.Add(comm_text);

                Button comm_send = new Button();
                comm_send.Content = "Add Comment"; 
                //comm_send.CommandParameter = stream.Id;
                comm_send.Click += (click_sender, click_e) => { comm_send_Click(sender, e, comm_text.Text, stream.Id.ToString()); };
                comm_panel.Children.Add(comm_send);

                Expander expan = new Expander();
                expan.Header = blk_link;
                expan.Content = comm_panel;
                para.Inlines.Add(expan);
                para.Inlines.Add(new LineBreak());
            }

            FlowDoc.Blocks.Add(para);
            RichSpaceDisplay.Document = FlowDoc;
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation da = new DoubleAnimation(-434,new Duration(new TimeSpan(15)));
            Storyboard.SetTargetProperty(da, new PropertyPath("(Window.Left)"));            //Do not miss the '(' and ')'
            sb.Children.Add(da);
            this.BeginStoryboard(sb);
        }

        private void comm_send_Click(object sender, EventArgs e, string comm, string streamid)
        {
            string requestUrl = Podio.API.Constants.PODIOAPI_BASEURL + "/comment/item/" + streamid + "/"; //this url will only work for app item
            Dictionary<string, string> commdata = new Dictionary<string, string>();
            commdata.Add("value", comm); 
            Podio.API.Utils.PodioRestHelper.PodioResponse response = Podio.API.Utils.PodioRestHelper.JSONRequest(requestUrl,client.AuthInfo.AccessToken, commdata, Podio.API.Utils.PodioRestHelper.RequestMethod.POST);
            Workspace_Click(sender,(RoutedEventArgs)e); //refresh
        }

        private Dictionary<string,object> get_push(string id, string type)
        {
            string requestUrl = Podio.API.Constants.PODIOAPI_BASEURL + "/" + type + "/" + id + "/"; 
            Podio.API.Utils.PodioRestHelper.PodioResponse response = Podio.API.Utils.PodioRestHelper.Request(requestUrl, client.AuthInfo.AccessToken);//GET by default

            string contents = response.Data;
            Dictionary<string, object> dic_all = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents);
            string push_info = dic_all["push"].ToString();
            Dictionary<string, object> dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(push_info);
            return dic;
        }

        private void link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}

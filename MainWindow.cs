namespace Phone
{
    public class MainWindow : global::System.Windows.Forms.Form
    {
        private global::System.Windows.Forms.TabPage DirectCallTab;
        private global::System.Windows.Forms.GroupBox groupBox3;
        private global::System.Windows.Forms.Button btnHangUpDirectCall;
        private global::System.Windows.Forms.Label label6;
        private global::System.Windows.Forms.Button btnMakeDirectCall;
        private global::System.Windows.Forms.TextBox tbTargetIP;
        private global::System.Windows.Forms.Label label5;
        private global::System.Windows.Forms.TextBox tbTargetUserNameDirect;
        private global::System.Windows.Forms.GroupBox groupBox1;
        private global::System.Windows.Forms.Button btnLogOut;
        private global::System.Windows.Forms.Button btnLogIn;
        private global::System.Windows.Forms.Label label1;
        private global::System.Windows.Forms.TextBox tbAccountUser;
        private global::System.Windows.Forms.TabControl TabsContainer;
        private global::System.Windows.Forms.ToolStripMenuItem Item1;
        private global::System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private global::System.Windows.Forms.MenuStrip MainMenu;
        private global::System.Windows.Forms.Button btnPause;
        private global::System.Windows.Forms.Button btnResume;
        private global::SIPLib.Del DelOutput;
        private global::SIPLib.DelRequest DelRequest1;
        private global::SIPLib.Listener Phone;
        private global::SIPLib.Player player = new global::SIPLib.Player("127.0.0.1", portplayer);
        private global::SIPLib.DelStopListener DelStoplistener;
        private static int portplayer = 4000;
        private static int port = 5060;
        private void funcOutput(string Info, string Caption) { global::System.Windows.Forms.MessageBox.Show(Info, Caption); }
        private void StopListenerFunc() { this.Phone.StopPhone(); }

        private bool ReceivedInvite(string a)
        {
            if (global::System.Windows.Forms.DialogResult.Yes == global::System.Windows.Forms.MessageBox.Show("Want to receive a call from: " + a, "Call Received", global::System.Windows.Forms.MessageBoxButtons.YesNo))
            {
                this.player.SetOptions(a.Remove(0, a.IndexOf('@') + 1), global::Phone.MainWindow.portplayer);
                this.player.Start();
                this.btnMakeDirectCall.Enabled = false;
                this.btnHangUpDirectCall.Enabled = true;
                this.btnPause.Enabled = true;
                this.btnResume.Enabled = false;
                return true;
            }
            else
            {
                this.player.Stop();
                return false;
            }
        }

        private void btnMakeDirectCall_Click(object sender, global::System.EventArgs e)
        {
            this.Phone.MakeCall(this.tbTargetIP.Text, this.tbTargetUserNameDirect.Text, this.tbAccountUser.Text);
            this.player.SetOptions(this.tbTargetIP.Text, global::Phone.MainWindow.portplayer);
            this.player.Start();
            this.btnMakeDirectCall.Enabled = false;
            this.btnHangUpDirectCall.Enabled = true;
            this.btnLogOut.Enabled = false;
            this.btnPause.Enabled = true;
            this.btnResume.Enabled = false;
            this.tbTargetIP.Enabled = false;
            this.tbTargetUserNameDirect.Enabled = false;
        }

        private void btnHangUpDirectCall_Click(object sender, global::System.EventArgs e)
        {
            this.btnMakeDirectCall.Enabled = true;
            this.btnHangUpDirectCall.Enabled = false;
            this.btnLogOut.Enabled = true;
            this.btnPause.Enabled = false;
            this.btnResume.Enabled = false;
            this.tbTargetIP.Enabled = true;
            this.tbTargetUserNameDirect.Enabled = true;
            this.Phone.StopPhone();
            this.player.Stop();
        }

        private void MainWindow_FormClosing(object sender, global::System.Windows.Forms.FormClosingEventArgs e)
        {
            try
            {
                this.Phone.StopPhone();
                this.player.Stop();
                global::System.Windows.Forms.Application.Exit();
            } catch { /* NOTHING */ }
        }

        private void btnLogIn_Click(object sender, global::System.EventArgs e)
        {
            this.Phone = new global::SIPLib.Listener(global::Phone.MainWindow.port, this.DelRequest1, this.tbAccountUser.Text, null, this.DelOutput, this.DelStoplistener);
            this.btnMakeDirectCall.Enabled = true;
            this.btnHangUpDirectCall.Enabled = false;
            this.btnLogIn.Enabled = false;
            this.btnLogOut.Enabled = true;
            this.tbAccountUser.Enabled = false;
        }

        private void CloseAndExit()
        {
            if (this.Phone != null) { this.Phone.StopPhone(); }
            global::System.Windows.Forms.Application.Exit();
        }

        private void exitToolStripMenuItem_Click(object sender, global::System.EventArgs e) { this.CloseAndExit(); }

        private void btnPause_Click(object sender, global::System.EventArgs e)
        {
            this.player.Stop();
            this.btnPause.Enabled = false;
            this.btnResume.Enabled = true;
        }

        private void btnResume_Click(object sender, global::System.EventArgs e)
        {
            this.player.Start();
            this.btnPause.Enabled = true;
            this.btnResume.Enabled = false;
        }

        private void btnLogOut_Click(object sender, global::System.EventArgs e)
        {
            this.Phone.StopPhone();
            this.btnMakeDirectCall.Enabled = false;
            this.btnHangUpDirectCall.Enabled = false;
            this.btnLogIn.Enabled = true;
            this.btnLogOut.Enabled = false;
            this.tbAccountUser.Enabled = true;
        }

        private void InitializeComponent()
        {
            this.DirectCallTab = new global::System.Windows.Forms.TabPage();
            this.groupBox3 = new global::System.Windows.Forms.GroupBox();
            this.btnResume = new global::System.Windows.Forms.Button();
            this.btnPause = new global::System.Windows.Forms.Button();
            this.btnHangUpDirectCall = new global::System.Windows.Forms.Button();
            this.label6 = new global::System.Windows.Forms.Label();
            this.btnMakeDirectCall = new global::System.Windows.Forms.Button();
            this.tbTargetIP = new global::System.Windows.Forms.TextBox();
            this.label5 = new global::System.Windows.Forms.Label();
            this.tbTargetUserNameDirect = new global::System.Windows.Forms.TextBox();
            this.groupBox1 = new global::System.Windows.Forms.GroupBox();
            this.btnLogOut = new global::System.Windows.Forms.Button();
            this.btnLogIn = new global::System.Windows.Forms.Button();
            this.label1 = new global::System.Windows.Forms.Label();
            this.tbAccountUser = new global::System.Windows.Forms.TextBox();
            this.TabsContainer = new global::System.Windows.Forms.TabControl();
            this.Item1 = new global::System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new global::System.Windows.Forms.ToolStripMenuItem();
            this.MainMenu = new global::System.Windows.Forms.MenuStrip();
            this.DirectCallTab.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.TabsContainer.SuspendLayout();
            this.MainMenu.SuspendLayout();
            this.SuspendLayout();
            this.DirectCallTab.Controls.Add(this.groupBox3);
            this.DirectCallTab.Controls.Add(this.groupBox1);
            this.DirectCallTab.Location = new global::System.Drawing.Point(4, 25);
            this.DirectCallTab.Name = "DirectCallTab";
            this.DirectCallTab.Padding = new global::System.Windows.Forms.Padding(3);
            this.DirectCallTab.Size = new global::System.Drawing.Size(303, 353);
            this.DirectCallTab.TabIndex = 0;
            this.DirectCallTab.Text = "Direct Call";
            this.DirectCallTab.UseVisualStyleBackColor = true;
            this.groupBox3.Controls.Add(this.btnResume);
            this.groupBox3.Controls.Add(this.btnPause);
            this.groupBox3.Controls.Add(this.btnHangUpDirectCall);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.btnMakeDirectCall);
            this.groupBox3.Controls.Add(this.tbTargetIP);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.tbTargetUserNameDirect);
            this.groupBox3.Location = new global::System.Drawing.Point(6, 157);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new global::System.Drawing.Size(287, 187);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Partner Properties";
            this.btnResume.Enabled = false;
            this.btnResume.Location = new global::System.Drawing.Point(6, 148);
            this.btnResume.Name = "btnResume";
            this.btnResume.Size = new global::System.Drawing.Size(101, 23);
            this.btnResume.TabIndex = 9;
            this.btnResume.Text = "Resume";
            this.btnResume.UseVisualStyleBackColor = true;
            this.btnResume.Click += new global::System.EventHandler(this.btnResume_Click);
            this.btnPause.Enabled = false;
            this.btnPause.Location = new global::System.Drawing.Point(6, 119);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new global::System.Drawing.Size(101, 23);
            this.btnPause.TabIndex = 8;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new global::System.EventHandler(this.btnPause_Click);
            this.btnHangUpDirectCall.Enabled = false;
            this.btnHangUpDirectCall.Location = new global::System.Drawing.Point(206, 93);
            this.btnHangUpDirectCall.Name = "btnHangUpDirectCall";
            this.btnHangUpDirectCall.Size = new global::System.Drawing.Size(75, 49);
            this.btnHangUpDirectCall.TabIndex = 5;
            this.btnHangUpDirectCall.Text = "Hang Up";
            this.btnHangUpDirectCall.UseVisualStyleBackColor = true;
            this.btnHangUpDirectCall.Click += new global::System.EventHandler(this.btnHangUpDirectCall_Click);
            this.label6.AutoSize = true;
            this.label6.Location = new global::System.Drawing.Point(6, 74);
            this.label6.Name = "label6";
            this.label6.Size = new global::System.Drawing.Size(61, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Your Address Is:";
            this.btnMakeDirectCall.Enabled = false;
            this.btnMakeDirectCall.Location = new global::System.Drawing.Point(206, 39);
            this.btnMakeDirectCall.Name = "btnMakeDirectCall";
            this.btnMakeDirectCall.Size = new global::System.Drawing.Size(75, 48);
            this.btnMakeDirectCall.TabIndex = 4;
            this.btnMakeDirectCall.Text = "Call";
            this.btnMakeDirectCall.UseVisualStyleBackColor = true;
            this.btnMakeDirectCall.Click += new global::System.EventHandler(this.btnMakeDirectCall_Click);
            this.tbTargetIP.Location = new global::System.Drawing.Point(6, 93);
            this.tbTargetIP.MaxLength = 500;
            this.tbTargetIP.Name = "tbTargetIP";
            this.tbTargetIP.Size = new global::System.Drawing.Size(137, 20);
            this.tbTargetIP.TabIndex = 4;
            this.tbTargetIP.Text = "192.168.1.101";
            this.label5.AutoSize = true;
            this.label5.Location = new global::System.Drawing.Point(6, 23);
            this.label5.Name = "label5";
            this.label5.Size = new global::System.Drawing.Size(106, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "User Name:";
            this.tbTargetUserNameDirect.Location = new global::System.Drawing.Point(6, 39);
            this.tbTargetUserNameDirect.Name = "tbTargetUserNameDirect";
            this.tbTargetUserNameDirect.Size = new global::System.Drawing.Size(137, 20);
            this.tbTargetUserNameDirect.TabIndex = 1;
            this.tbTargetUserNameDirect.Text = "USER2";
            this.groupBox1.Controls.Add(this.btnLogOut);
            this.groupBox1.Controls.Add(this.btnLogIn);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbAccountUser);
            this.groupBox1.Location = new global::System.Drawing.Point(6, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new global::System.Drawing.Size(287, 145);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Your Account";
            this.btnLogOut.Enabled = false;
            this.btnLogOut.Location = new global::System.Drawing.Point(31, 104);
            this.btnLogOut.Name = "btnLogOut";
            this.btnLogOut.Size = new global::System.Drawing.Size(88, 23);
            this.btnLogOut.TabIndex = 4;
            this.btnLogOut.Text = "Log Out";
            this.btnLogOut.UseVisualStyleBackColor = true;
            this.btnLogOut.Click += new global::System.EventHandler(this.btnLogOut_Click);
            this.btnLogIn.Location = new global::System.Drawing.Point(31, 71);
            this.btnLogIn.Name = "btnLogIn";
            this.btnLogIn.Size = new global::System.Drawing.Size(88, 23);
            this.btnLogIn.TabIndex = 4;
            this.btnLogIn.Text = "Log In";
            this.btnLogIn.UseVisualStyleBackColor = true;
            this.btnLogIn.Click += new global::System.EventHandler(this.btnLogIn_Click);
            this.label1.AutoSize = true;
            this.label1.Location = new global::System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new global::System.Drawing.Size(106, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "User Name:";
            this.tbAccountUser.Location = new global::System.Drawing.Point(6, 39);
            this.tbAccountUser.Name = "tbAccountUser";
            this.tbAccountUser.Size = new global::System.Drawing.Size(137, 20);
            this.tbAccountUser.TabIndex = 0;
            this.tbAccountUser.Text = "USER1";
            this.TabsContainer.Appearance = global::System.Windows.Forms.TabAppearance.Buttons;
            this.TabsContainer.Controls.Add(this.DirectCallTab);
            this.TabsContainer.Dock = global::System.Windows.Forms.DockStyle.Fill;
            this.TabsContainer.Location = new global::System.Drawing.Point(0, 24);
            this.TabsContainer.Name = "TabsContainer";
            this.TabsContainer.SelectedIndex = 0;
            this.TabsContainer.Size = new global::System.Drawing.Size(311, 382);
            this.TabsContainer.TabIndex = 0;
            this.Item1.DropDownItems.AddRange(new global::System.Windows.Forms.ToolStripItem[] { this.exitToolStripMenuItem });
            this.Item1.Name = "Item1";
            this.Item1.Size = new global::System.Drawing.Size(61, 20);
            this.Item1.Text = "Main";
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new global::System.Drawing.Size(117, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new global::System.EventHandler(this.exitToolStripMenuItem_Click);
            this.MainMenu.BackColor = global::System.Drawing.SystemColors.Control;
            this.MainMenu.Items.AddRange(new global::System.Windows.Forms.ToolStripItem[] { this.Item1 });
            this.MainMenu.Location = new global::System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new global::System.Drawing.Size(311, 24);
            this.MainMenu.TabIndex = 1;
            this.MainMenu.Text = "menuStrip1";
            this.AutoScaleDimensions = new global::System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new global::System.Drawing.Size(311, 406);
            this.Controls.Add(this.TabsContainer);
            this.Controls.Add(this.MainMenu);
            this.FormBorderStyle = global::System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.MainMenu;
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.Text = "Main Window";
            this.FormClosing += new global::System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.DirectCallTab.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.TabsContainer.ResumeLayout(false);
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.CloseAndExit();
        }

        public MainWindow() : base()
        {
            this.InitializeComponent();
            this.DelOutput += funcOutput;
            this.DelRequest1 += ReceivedInvite;
            this.DelStoplistener += DelStoplistener;
        }

        ~MainWindow() { this.Dispose(); }
        internal static void Main(string[] args) { global::System.Windows.Forms.Application.Run(new global::Phone.MainWindow()); }
    }
}

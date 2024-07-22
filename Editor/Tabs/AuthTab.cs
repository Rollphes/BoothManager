using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using io.github.rollphes.boothManager.client;

namespace io.github.rollphes.boothManager.tabs {
    internal class AuthTab : TabBase {
        private static readonly VisualTreeAsset _loginFormUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginForm");
        private static readonly VisualTreeAsset _loginFormExecutionStatusUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginFormExecutionStatus");
        private static readonly VisualTreeAsset _loginSuccessUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginSuccess");

        internal override string Tooltip => "”FØ";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Login");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/AuthTabContent");

        private VisualElement _loginForm;

        public AuthTab(Client client, TabController tabController, VisualElement tabContent) : base(client, tabController, tabContent) { }

        internal override void Show() {
            base.Show();
            this._loginForm = this._tabContent.Q<VisualElement>("LoginForm");

            if (this._client.IsDeployed == false) {
                _loginFormExecutionStatusUxml.CloneTree(this._loginForm);
                var statusLabel = this._loginForm.Q<Label>("ExecutionStatus");
                statusLabel.text = "Trying to auto sign in...";

                this._tabController._IsLock = true;

                this._client.AfterDeploy += async () => {
                    if (this._client.IsLoggedIn) {
                        statusLabel.text = "Data Fetching...";
                        await this._client.FetchItemInfos();
                        this.ShowLoginSuccess();
                    } else {
                        this.ShowLoginForm();
                    }
                    this._tabController._IsLock = false;
                };
            } else {
                if (this._client.IsLoggedIn) {
                    this.ShowLoginSuccess();
                } else {
                    this.ShowLoginForm();
                }
            }
        }

        private void ShowLoginForm() {
            this._loginForm.Clear();
            _loginFormUxml.CloneTree(this._loginForm);

            var signInButton = this._loginForm.Q<Button>("SignInButton");
            var signUpButton = this._loginForm.Q<Button>("SignUpButton");
            var emailField = this._loginForm.Q<TextField>("EmailField");
            var passwordField = this._loginForm.Q<TextField>("PasswordField");

            signInButton.clicked += async () => await this.HandleSignIn(emailField.value, passwordField.value);
            signUpButton.clicked += () => this._client.SignUp();
        }

        private void ShowLoginSuccess() {
            this._loginForm.Clear();
            _loginSuccessUxml.CloneTree(this._loginForm);

            var nickNameLabel = this._loginForm.Q<Label>("NickNameLabel");
            nickNameLabel.text = this._client.NickName;

            var logoutButton = this._loginForm.Q<Button>("LogoutButton");
            logoutButton.clicked += () => {
                this._client.SignOut();
                this.Show();
            };
        }

        private async System.Threading.Tasks.Task HandleSignIn(string email, string password) {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                EditorUtility.DisplayDialog("Error logging in", "Please enter a valid username and password", "OK");
                return;
            }

            this._loginForm.Clear();
            _loginFormExecutionStatusUxml.CloneTree(this._loginForm);
            var statusLabel = this._loginForm.Q<Label>("ExecutionStatus");
            statusLabel.text = "Trying to sign in...";


            try {
                await this._client.SignIn(email, password);
                statusLabel.text = "Fetching Data...";
                await this._client.FetchItemInfos();
                this.ShowLoginSuccess();
            } catch (Exception e) {
                Debug.LogError(e.Message);
                EditorUtility.DisplayDialog("Error logging in", "Invalid Username/Email or Password", "OK");
                this.Show();
            }
        }
    }
}
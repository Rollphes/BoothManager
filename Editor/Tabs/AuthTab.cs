using System;

using io.github.rollphes.epmanager.booth;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.rollphes.epmanager.tabs {
    internal class AuthTab : TabBase {
        private static readonly VisualTreeAsset _loginFormUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginForm");
        private static readonly VisualTreeAsset _loginFormExecutionStatusUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginFormExecutionStatus");
        private static readonly VisualTreeAsset _loginSuccessUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginSuccess");

        internal override string Tooltip => "認証";
        internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Login");

        protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/AuthTabContent");

        private VisualElement _loginForm;

        internal AuthTab(MainWindow window) : base(window) { }

        internal override void Show() {
            base.Show();

            this._loginForm = this._tabContent.Q<VisualElement>("LoginForm");

            if (BoothClient.IsDeployed == false) {
                _loginFormExecutionStatusUxml.CloneTree(this._loginForm);

                var progressBar = this._loginForm.Q<ProgressBar>("Progress");
                var statusLabel = this._loginForm.Q<Label>("ExecutionStatus");
                statusLabel.text = "Trying to auto sign in...";
                TabController.IsLock = true;

                BoothClient.OnDeployProgressing += async (deployStatusType) => {
                    switch (deployStatusType) {
                        case DeployStatusType.BrowserDownloading:
                            statusLabel.text = "Browser Downloading...";
                            break;
                        case DeployStatusType.AutoLoginInProgress:
                            statusLabel.text = "Trying to auto sign in...";
                            break;
                        case DeployStatusType.Complete:
                            if (BoothClient.IsLoggedIn) {
                                statusLabel.text = "Last page count fetching...";
                                await BoothClient.FetchItemInfos(false, (status, index, length) => {
                                    this.FetchItemInfoOnProgressHandle(statusLabel, progressBar, status, index, length);
                                });

                                this.ShowLoginSuccess();
                            } else {
                                this.ShowLoginForm();
                            }

                            TabController.IsLock = false;
                            break;
                    }
                };
            } else {
                if (BoothClient.IsLoggedIn) {
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

            signInButton.clicked += async () => await this.SignInHandle(emailField.value, passwordField.value);
            signUpButton.clicked += () => BoothClient.SignUp();
        }

        private void ShowLoginSuccess() {
            this._loginForm.Clear();
            _loginSuccessUxml.CloneTree(this._loginForm);

            var nickNameLabel = this._loginForm.Q<Label>("NickNameLabel");
            nickNameLabel.text = BoothClient.NickName;

            var logoutButton = this._loginForm.Q<Button>("LogoutButton");
            logoutButton.clicked += () => {
                BoothClient.SignOut();
                this.Show();
            };
        }

        private async System.Threading.Tasks.Task SignInHandle(string email, string password) {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
                EditorUtility.DisplayDialog("Error logging in", "Please enter a valid username and password", "OK");
                return;
            }

            this._loginForm.Clear();
            _loginFormExecutionStatusUxml.CloneTree(this._loginForm);

            var progressBar = this._loginForm.Q<ProgressBar>("Progress");
            var statusLabel = this._loginForm.Q<Label>("ExecutionStatus");
            statusLabel.text = "Trying to sign in...";

            try {
                await BoothClient.SignIn(email, password);

                statusLabel.text = "Last page count fetching...";
                await BoothClient.FetchItemInfos(false, (status, index, length) => {
                    this.FetchItemInfoOnProgressHandle(statusLabel, progressBar, status, index, length);
                });

                this.ShowLoginSuccess();
            } catch (Exception e) {
                Debug.LogError(e.Message);
                EditorUtility.DisplayDialog("Error logging in", "Invalid Username/Email or Password", "OK");

                this.Show();
            }
        }

        private void FetchItemInfoOnProgressHandle(Label label, ProgressBar progress, FetchItemInfoStatusType status, int index, int length) {
            progress.style.display = DisplayStyle.Flex;
            progress.highValue = length;
            progress.lowValue = 0;
            progress.value = index;

            switch (status) {
                case FetchItemInfoStatusType.ItemIdFetchingInLibrary:
                    label.text = "ItemId Fetching In Library page...";
                    progress.title = $"{index}/{length} page";
                    break;
                case FetchItemInfoStatusType.ItemIdFetchingInGift:
                    label.text = "ItemId Fetching In Gift page...";
                    progress.title = $"{index}/{length} page";
                    break;
                case FetchItemInfoStatusType.ItemInfoFetching:
                    label.text = "Item Info Fetching...";
                    progress.title = $"{index}/{length} item";
                    break;
            }
        }
    }
}
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal class AuthTab : TabBase {
    private static readonly VisualTreeAsset _loginFormUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginForm");
    private static readonly VisualTreeAsset _loginFormTryingUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginFormTrying");
    private static readonly VisualTreeAsset _loginSuccessUxml = Resources.Load<VisualTreeAsset>("UI/Components/LoginSuccess");

    internal override string Tooltip => "”FØ";
    internal override Texture2D TabIcon => Resources.Load<Texture2D>("UI/Icons/Login");

    protected override VisualTreeAsset InitTabUxml => Resources.Load<VisualTreeAsset>("UI/Tabs/AuthTabContent");

    private VisualElement _loginForm;

    internal AuthTab(Client client, VisualElement tabContent) : base(client, tabContent) { }

    internal override void Show() {
        base.Show();

        this._loginForm = this._tabContent.Q<VisualElement>("LoginForm");
        _loginFormUxml.CloneTree(this._loginForm);

        var signInButton = this._loginForm.Q<Button>("SignInButton");
        var signUpButton = this._loginForm.Q<Button>("SignUpButton");
        var emailField = this._loginForm.Q<TextField>("EmailField");
        var passwordField = this._loginForm.Q<TextField>("PasswordField");

        if (_client.IsDeployed == false) {
            _client.AfterDeploy += this.CheckAutoLogin;
        } else {
            this.CheckAutoLogin();
        }


        signInButton.clicked += async () => await this.HandleSignIn(emailField.value, passwordField.value);
        signUpButton.clicked += () => this._client.SignUp();
    }

    internal void ShowLoginSuccess() {
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
        _loginFormTryingUxml.CloneTree(this._loginForm);


        try {
            await this._client.SignIn(email, password);
            this.ShowLoginSuccess();
        } catch (Exception e) {
            Debug.LogError(e.Message);
            EditorUtility.DisplayDialog("Error logging in", "Invalid Username/Email or Password", "OK");
            this.Show();
        }
    }

    private void CheckAutoLogin() {
        if (_client.IsLoggedIn) {
            Debug.Log("AutoLogin Check Done!");
            this.ShowLoginSuccess();
        } else {
            Debug.Log("AutoLogin Check Failed!");
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StreetFoo.Client
{
    // concrete implementation of the RegisterPage's view-model...
    public class RegisterPageViewModel : ViewModel, IRegisterPageViewModel
    {
        // commands...
        public ICommand RegisterCommand { get; private set; }

        public RegisterPageViewModel(IViewModelHost host)
            : base(host)
        {
            // set RegisterCommand to defer to the DoRegistration method...
            this.RegisterCommand = new DelegateCommand((args) => DoRegistration(args as CommandExecutionContext));
        }

        public string Username
        {
            get
            {
                // the magic CallerMemberNameAttribute automatically maps this to a
                // hash key of "Username"...
                return this.GetValue<string>();
            }
            set
            {
                // likewise, CallerMemberNameAttribute works here too...
                this.SetValue(value);
            }
        }

        public string Email
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }

        public string Password
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }

        public string Confirm
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }

        private void DoRegistration(CommandExecutionContext context)
        {
            // if we don't have a context, create one...
            if (context == null)
                context = new CommandExecutionContext();

            // validate...
            ErrorBucket errors = new ErrorBucket();
            Validate(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                // get a handler...
                IRegisterServiceProxy proxy = ServiceProxyFactory.Current.GetHandler<IRegisterServiceProxy>();

                // call...
                this.EnterBusy();
                var task = proxy.Register(this.Username, this.Email, this.Password, this.Confirm, async (result) =>
                {
                    // show a message to say that a user has been created... (this isn't a helpful message, 
                    // included for illustration...)
                    await this.Host.ShowAlertAsync(string.Format("The new user has been created.\r\n\r\nUser ID: {0}", result.UserId));

                    // navigate to the logon page...
                    this.Host.ShowView(typeof(ILogonPageViewModel));

                }, this.GetFailureHandler(), this.GetCompleteHandler(true));

                // add...
                context.AddTask(task);
            }

            // errors?
            if(errors.HasErrors)
                this.Host.ShowAlertAsync(errors);
        }

        private void Validate(ErrorBucket errors)
        {
            // do basic data presence validation...
            if (string.IsNullOrEmpty(Username))
                errors.AddError("Username is required.");
            if (string.IsNullOrEmpty(Email))
                errors.AddError("Email is required.");
            if (string.IsNullOrEmpty(Password))
                errors.AddError("Password is required.");
            if (string.IsNullOrEmpty(Confirm))
                errors.AddError("Confirm password is required.");

            // check the passwords...
            if (!(string.IsNullOrEmpty(Password)) && this.Password != this.Confirm)
                errors.AddError("The passwords do not match.");
        }
    }
}
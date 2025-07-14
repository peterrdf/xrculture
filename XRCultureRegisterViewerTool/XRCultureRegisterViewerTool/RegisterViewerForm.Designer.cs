namespace XRCultureRegisterViewerTool
{
    partial class RegisterViewerForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _textBoxMiddleware = new TextBox();
            label1 = new Label();
            _buttonRegister = new Button();
            _buttonAuthorize = new Button();
            _textBoxLog = new TextBox();
            _buttonClose = new Button();
            _buttonViewModel = new Button();
            SuspendLayout();
            // 
            // _textBoxMiddleware
            // 
            _textBoxMiddleware.Location = new Point(87, 13);
            _textBoxMiddleware.Name = "_textBoxMiddleware";
            _textBoxMiddleware.Size = new Size(255, 23);
            _textBoxMiddleware.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 17);
            label1.Name = "label1";
            label1.Size = new Size(72, 15);
            label1.TabIndex = 1;
            label1.Text = "Middleware:";
            // 
            // _buttonRegister
            // 
            _buttonRegister.Enabled = false;
            _buttonRegister.Location = new Point(483, 17);
            _buttonRegister.Name = "_buttonRegister";
            _buttonRegister.Size = new Size(120, 23);
            _buttonRegister.TabIndex = 2;
            _buttonRegister.Text = "Register";
            _buttonRegister.UseVisualStyleBackColor = true;
            _buttonRegister.Click += _buttonRegister_Click;
            // 
            // _buttonAuthorize
            // 
            _buttonAuthorize.Location = new Point(348, 17);
            _buttonAuthorize.Name = "_buttonAuthorize";
            _buttonAuthorize.Size = new Size(128, 23);
            _buttonAuthorize.TabIndex = 3;
            _buttonAuthorize.Text = "Authorize";
            _buttonAuthorize.UseVisualStyleBackColor = true;
            _buttonAuthorize.Click += _buttonAuthorize_Click;
            // 
            // _textBoxLog
            // 
            _textBoxLog.Location = new Point(11, 42);
            _textBoxLog.Multiline = true;
            _textBoxLog.Name = "_textBoxLog";
            _textBoxLog.ReadOnly = true;
            _textBoxLog.Size = new Size(592, 367);
            _textBoxLog.TabIndex = 4;
            // 
            // _buttonClose
            // 
            _buttonClose.Location = new Point(528, 415);
            _buttonClose.Name = "_buttonClose";
            _buttonClose.Size = new Size(75, 23);
            _buttonClose.TabIndex = 5;
            _buttonClose.Text = "Close";
            _buttonClose.UseVisualStyleBackColor = true;
            _buttonClose.Click += _buttonClose_Click;
            // 
            // _buttonViewModel
            // 
            _buttonViewModel.Location = new Point(12, 415);
            _buttonViewModel.Name = "_buttonViewModel";
            _buttonViewModel.Size = new Size(120, 23);
            _buttonViewModel.TabIndex = 6;
            _buttonViewModel.Text = "View Model";
            _buttonViewModel.UseVisualStyleBackColor = true;
            _buttonViewModel.Click += _buttonViewModel_Click;
            // 
            // RegisterViewerForm
            // 
            AcceptButton = _buttonClose;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(615, 447);
            ControlBox = false;
            Controls.Add(_buttonViewModel);
            Controls.Add(_buttonClose);
            Controls.Add(_buttonAuthorize);
            Controls.Add(_buttonRegister);
            Controls.Add(label1);
            Controls.Add(_textBoxMiddleware);
            Controls.Add(_textBoxLog);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "RegisterViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "XRCulture Register Viewer Tool";
            Load += RegisterViewerForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox _textBoxMiddleware;
        private Label label1;
        private Button _buttonRegister;
        private Button _buttonAuthorize;
        private TextBox _textBoxLog;
        private Button _buttonClose;
        private Button _buttonViewModel;
    }
}

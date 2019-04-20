namespace TestBench
{
	partial class Form1
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.vtWindow1 = new TeraTrem.VTWindow();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.cmbAudioList = new System.Windows.Forms.ComboBox();
			this.cmbVideoList = new System.Windows.Forms.ComboBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel1 = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Location = new System.Drawing.Point(3, 3);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(480, 272);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
			this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
			this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 68);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(21, 12);
			this.label1.TabIndex = 1;
			this.label1.Text = "red";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 84);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(33, 12);
			this.label2.TabIndex = 2;
			this.label2.Text = "green";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 100);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(26, 12);
			this.label3.TabIndex = 3;
			this.label3.Text = "blue";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 116);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(27, 12);
			this.label4.TabIndex = 4;
			this.label4.Text = "user";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(8, 140);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 5;
			this.button1.Text = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// vtWindow1
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.vtWindow1, 2);
			this.vtWindow1.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.vtWindow1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.vtWindow1.Location = new System.Drawing.Point(3, 281);
			this.vtWindow1.Name = "vtWindow1";
			this.vtWindow1.Size = new System.Drawing.Size(629, 166);
			this.vtWindow1.TabIndex = 6;
			this.vtWindow1.Text = "vtWindow1";
			// 
			// timer1
			// 
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// cmbAudioList
			// 
			this.cmbAudioList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cmbAudioList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbAudioList.FormattingEnabled = true;
			this.cmbAudioList.Location = new System.Drawing.Point(8, 8);
			this.cmbAudioList.Name = "cmbAudioList";
			this.cmbAudioList.Size = new System.Drawing.Size(126, 20);
			this.cmbAudioList.TabIndex = 7;
			// 
			// cmbVideoList
			// 
			this.cmbVideoList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cmbVideoList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbVideoList.FormattingEnabled = true;
			this.cmbVideoList.Location = new System.Drawing.Point(8, 40);
			this.cmbVideoList.Name = "cmbVideoList";
			this.cmbVideoList.Size = new System.Drawing.Size(126, 20);
			this.cmbVideoList.TabIndex = 8;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 486F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.vtWindow1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 278F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(635, 450);
			this.tableLayoutPanel1.TabIndex = 9;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.cmbAudioList);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.cmbVideoList);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.button1);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Controls.Add(this.label4);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(489, 3);
			this.panel1.MinimumSize = new System.Drawing.Size(140, 272);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(143, 272);
			this.panel1.TabIndex = 10;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(635, 450);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button button1;
		private TeraTrem.VTWindow vtWindow1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ComboBox cmbAudioList;
		private System.Windows.Forms.ComboBox cmbVideoList;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Panel panel1;
	}
}


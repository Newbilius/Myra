/* Generated by MyraPad at 7/12/2021 2:16:12 AM */
using Myra.Graphics2D.UI;

namespace Myra.Samples.TextRendering.UI
{
	partial class MainPanel: Panel
	{
		private void BuildUI()
		{
			var horizontalSeparator1 = new HorizontalSeparator();

			var label1 = new Label();
			label1.Text = "Font Size:";

			_spinButtonFontSize = new SpinButton();
			_spinButtonFontSize.Value = 32;
			_spinButtonFontSize.Width = 40;
			_spinButtonFontSize.Id = "_spinButtonFontSize";

			var label2 = new Label();
			label2.Text = "Scale:";

			var label3 = new Label();
			label3.Text = "0.1";

			_sliderScale = new HorizontalSlider();
			_sliderScale.Minimum = 0.1f;
			_sliderScale.Maximum = 10;
			_sliderScale.Value = 1;
			_sliderScale.Width = 200;
			_sliderScale.Id = "_sliderScale";

			_labelScaleValue = new Label();
			_labelScaleValue.Text = "5.4";
			_labelScaleValue.HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment.Center;
			_labelScaleValue.Id = "_labelScaleValue";

			var verticalStackPanel1 = new VerticalStackPanel();
			verticalStackPanel1.Widgets.Add(_sliderScale);
			verticalStackPanel1.Widgets.Add(_labelScaleValue);

			var label4 = new Label();
			label4.Text = "10";

			_buttonReset = new TextButton();
			_buttonReset.Text = "Reset";
			_buttonReset.Id = "_buttonReset";

			_checkBoxSmoothText = new CheckBox();
			_checkBoxSmoothText.Text = "Smooth Text";
			_checkBoxSmoothText.Id = "_checkBoxSmoothText";

			_checkBoxShowTexture = new CheckBox();
			_checkBoxShowTexture.IsChecked = true;
			_checkBoxShowTexture.Text = "Show Texture";
			_checkBoxShowTexture.Id = "_checkBoxShowTexture";

			var horizontalStackPanel1 = new HorizontalStackPanel();
			horizontalStackPanel1.Spacing = 8;
			horizontalStackPanel1.Widgets.Add(label1);
			horizontalStackPanel1.Widgets.Add(_spinButtonFontSize);
			horizontalStackPanel1.Widgets.Add(label2);
			horizontalStackPanel1.Widgets.Add(label3);
			horizontalStackPanel1.Widgets.Add(verticalStackPanel1);
			horizontalStackPanel1.Widgets.Add(label4);
			horizontalStackPanel1.Widgets.Add(_buttonReset);
			horizontalStackPanel1.Widgets.Add(_checkBoxSmoothText);
			horizontalStackPanel1.Widgets.Add(_checkBoxShowTexture);

			var horizontalSeparator2 = new HorizontalSeparator();

			_textBoxText = new TextBox();
			_textBoxText.Text = "The quick brown\\nfox jumps over\\nthe lazy dog";
			_textBoxText.Multiline = true;
			_textBoxText.Wrap = true;
			_textBoxText.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Stretch;
			_textBoxText.Id = "_textBoxText";

			var scrollViewer1 = new ScrollViewer();
			scrollViewer1.Content = _textBoxText;

			_imageTexture = new Image();
			_imageTexture.Id = "_imageTexture";

			var horizontalStackPanel2 = new HorizontalStackPanel();
			horizontalStackPanel2.DefaultProportion = new Proportion
			{
				Type = Myra.Graphics2D.UI.ProportionType.Part,
			};
			horizontalStackPanel2.Widgets.Add(scrollViewer1);
			horizontalStackPanel2.Widgets.Add(_imageTexture);

			var verticalStackPanel2 = new VerticalStackPanel();
			verticalStackPanel2.Proportions.Add(new Proportion
			{
				Type = Myra.Graphics2D.UI.ProportionType.Auto,
			});
			verticalStackPanel2.Proportions.Add(new Proportion
			{
				Type = Myra.Graphics2D.UI.ProportionType.Auto,
			});
			verticalStackPanel2.Proportions.Add(new Proportion
			{
				Type = Myra.Graphics2D.UI.ProportionType.Auto,
			});
			verticalStackPanel2.Proportions.Add(new Proportion
			{
				Type = Myra.Graphics2D.UI.ProportionType.Fill,
			});
			verticalStackPanel2.Widgets.Add(horizontalSeparator1);
			verticalStackPanel2.Widgets.Add(horizontalStackPanel1);
			verticalStackPanel2.Widgets.Add(horizontalSeparator2);
			verticalStackPanel2.Widgets.Add(horizontalStackPanel2);

			
			Widgets.Add(verticalStackPanel2);
		}

		
		public SpinButton _spinButtonFontSize;
		public HorizontalSlider _sliderScale;
		public Label _labelScaleValue;
		public TextButton _buttonReset;
		public CheckBox _checkBoxSmoothText;
		public CheckBox _checkBoxShowTexture;
		public TextBox _textBoxText;
		public Image _imageTexture;
	}
}

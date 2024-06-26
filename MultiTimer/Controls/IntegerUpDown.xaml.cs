﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultiTimer.Controls
{
    /// <summary>
    /// IntegerUpDown.xaml の相互作用ロジック
    /// </summary>
    public partial class IntegerUpDown : UserControl
    {
        
        public IntegerUpDown()
        {
            InitializeComponent();
        }

        #region Dependency Property: CurrentValue
        public int CurrentValue
        {
            get { return (int)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        private static void OnCurrentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var integerUpDown = (IntegerUpDown)d;
            int forcedNewValue = WithinRange(integerUpDown.CurrentValue, integerUpDown.MinValue, integerUpDown.MaxValue);
            if (forcedNewValue != integerUpDown.CurrentValue)
            {
                integerUpDown.CurrentValue = forcedNewValue;
            }
        }

        public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register(
            name: nameof(CurrentValue),
            propertyType: typeof(int),
            ownerType: typeof(IntegerUpDown),
            typeMetadata: new FrameworkPropertyMetadata(
                defaultValue: 0,
                flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                propertyChangedCallback: new PropertyChangedCallback(OnCurrentValueChanged)
            )
        );
        #endregion

        #region Dependency Property: Step
        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        private static bool IsValidStep(object value)
        {
            return (int)value > 0;
        }

        public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
            name: nameof(Step), 
            propertyType: typeof(int), 
            ownerType: typeof(IntegerUpDown), 
            typeMetadata: new PropertyMetadata(1),
            validateValueCallback: new ValidateValueCallback(IsValidStep)
        );
        #endregion

        #region Dependency Property: MinValue
        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        private static void OnMinValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var integerUpDown = (IntegerUpDown)d;
            // MaxValueを超える値をセットしたときはMaxValueと同じ値をセットしたことにする
            if (integerUpDown.MinValue > integerUpDown.MaxValue)
            {
                integerUpDown.MinValue = integerUpDown.MaxValue;
            }
            if (integerUpDown.CurrentValue < integerUpDown.MinValue)
            {
                integerUpDown.CurrentValue = integerUpDown.MinValue;
            }
        }

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            name: nameof(MinValue), 
            propertyType: typeof(int), 
            ownerType: typeof(IntegerUpDown), 
            typeMetadata: new PropertyMetadata(
                defaultValue: int.MinValue,
                propertyChangedCallback: new PropertyChangedCallback(OnMinValueChanged)
            )
        );
        #endregion

        #region Dependency Property: MaxValue
        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        private static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var integerUpDown = (IntegerUpDown)d;
            // MinValue未満の値をセットしたときはMinValueと同じ値をセットしたことにする
            if (integerUpDown.MaxValue < integerUpDown.MinValue)
            {
                integerUpDown.MaxValue = integerUpDown.MinValue;
            }
            if (integerUpDown.CurrentValue > integerUpDown.MaxValue)
            {
                integerUpDown.CurrentValue = integerUpDown.MaxValue;
            }
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            name: nameof(MaxValue),
            propertyType: typeof(int),
            ownerType: typeof(IntegerUpDown),
            typeMetadata: new PropertyMetadata(
                defaultValue: int.MaxValue,
                propertyChangedCallback: new PropertyChangedCallback(OnMaxValueChanged)
            )
        );
        #endregion

        #region Textbox Event Methods
        private void ValueTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // スペースキーを無効にする
            if (e.Key == Key.Space) e.Handled = true;
        }

        private void ValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            // 入力を反映させた文字列を実際に作ってみて、それが条件に合わなければ入力を弾く
            string newText = textbox.SelectionLength > 0 ? string.Concat(
                    textbox.Text.AsSpan(0, textbox.SelectionStart),
                    e.Text,
                    textbox.Text.AsSpan(textbox.SelectionStart + textbox.SelectionLength)
                ) : string.Concat(
                    textbox.Text.AsSpan(0, textbox.CaretIndex),
                    e.Text,
                    textbox.Text.AsSpan(textbox.CaretIndex)
                );
            if (!integerExtraZeroRegex().IsMatch(newText))
            {
                e.Handled = true;
                return;
            }
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            if (int.TryParse(textbox.Text, out int value))
            {
                textbox.Text = value.ToString();
            }
            else
            {
                textbox.Text = "0";
            }
        }

        private void ValueTextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // ペーストを無効にする
            if (e.Command == ApplicationCommands.Paste) e.Handled = true;
        }

        private void ValueTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            // ValueTextBoxがフォーカスを得たときにテキストを全選択する
            textbox.SelectAll();
        }

        private void ValueTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            // フォーカスがないときに左クリックされたらフォーカスを得る
            // （GotFocusイベントによりテキストが全選択状態になる）
            // ※フォーカスがないときに限定しないとマウスクリックによるキャレットの移動ができなくなってしまう
            if (!textbox.IsFocused)
            {
                textbox.Focus();
                // キャレットが設定されないようにMouseLeftButtonイベントを発生させないようにする
                e.Handled = true;
            }
        }
        #endregion

        #region Buttons Event Methods
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentValue += this.Step;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentValue -= this.Step;
        }
        #endregion


        #region Utils
        [GeneratedRegex(@"^-?\d*$")]
        private static partial Regex integerExtraZeroRegex();

        [GeneratedRegex(@"^0|[1-9]\d*|-[1-9]\d*$")]
        private static partial Regex integerRegex();

        private static int WithinRange(int value, int min, int max) => value < min ? min : value > max ? max : value;
        #endregion
    }
}

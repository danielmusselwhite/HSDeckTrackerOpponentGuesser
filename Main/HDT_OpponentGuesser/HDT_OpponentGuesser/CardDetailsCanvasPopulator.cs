using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace HDT_OpponentGuesser
{
    // Class for populating the CardDetailCanvas inside BestFitDeck.xaml with the currently hovered card's info
    public static class CardDetailsCanvasPopulator
    {
        public static void populateCardDetails(Canvas canvasCardDetails, string cardNameText, int cardManaValue, string cardHealthText,
        string cardAttackText, string cardDescriptionText, string cardTypeText, string rarity, Brush typeColor)
        {
            Dictionary<string, SolidColorBrush> typeColorDict = new Dictionary<string, SolidColorBrush>();
            typeColorDict.Add("MINION", Brushes.RosyBrown);
            typeColorDict.Add("SPELL", Brushes.PaleTurquoise);
            typeColorDict.Add("SECRET", Brushes.LightPink);
            typeColorDict.Add("WEAPON", Brushes.Beige);
            typeColorDict.Add("LOCATION", Brushes.DarkSeaGreen);

            // Create and configure the canvas
            canvasCardDetails.Name = "canvasCardDetails";
            canvasCardDetails.Background = typeColorDict[cardTypeText];
            canvasCardDetails.Margin = new Thickness(479, 432, 125, 146);

            // Create and configure the text blocks
            var CardName = new TextBlock
            {
                Name = "cardName",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 187
            };
            CardName.Text = cardNameText;
            Canvas.SetLeft(CardName, 0);
            Canvas.SetTop(CardName, 33);
            canvasCardDetails.Children.Add(CardName);

            var CardMana = new TextBlock
            {
                Name = "cardMana",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
                Foreground = Brushes.White,
                Background = Brushes.DarkCyan,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 57,
                Height = 27
            };
            CardMana.Text = cardManaValue.ToString() + " 💎";
            Canvas.SetLeft(CardMana, 0);
            Canvas.SetTop(CardMana, 0);
            canvasCardDetails.Children.Add(CardMana);

            // if type is minion, show health and attack
            if (cardTypeText == "MINION")
            {
                var CardHealth = new TextBlock
                {
                    Name = "cardHealth",
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 18,
                    Foreground = Brushes.White,
                    Background = Brushes.DarkRed,
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Width = 93,
                    Height = 27
                };
                CardHealth.Text = cardHealthText + " ♡";
                Canvas.SetLeft(CardHealth, 94);
                Canvas.SetTop(CardHealth, 228);
                canvasCardDetails.Children.Add(CardHealth);

                var CardAttack = new TextBlock
                {
                    Name = "cardAttack",
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 18,
                    Foreground = Brushes.White,
                    Background = Brushes.DarkGoldenrod,
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Width = 93,
                    Height = 27
                };
                CardAttack.Text = cardAttackText + " ⚔";
                Canvas.SetLeft(CardAttack, 0);
                Canvas.SetTop(CardAttack, 228);
                canvasCardDetails.Children.Add(CardAttack);
            }

            var CardDescription = new TextBlock
            {
                Name = "cardDescription",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 10,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 177,
                Height = 104
            };

            // remove any @ and $ symbols
            cardDescriptionText = cardDescriptionText.Replace("@", "").Replace("$", "").Replace("{", "").Replace("}", "");
            // remove anything inside of <>
            CardDescription.Text = Regex.Replace(Regex.Replace(cardDescriptionText, @"<[^>]*>", String.Empty), @"\[[^]]*\]", String.Empty);


            Canvas.SetLeft(CardDescription, 5);
            Canvas.SetTop(CardDescription, 108);
            canvasCardDetails.Children.Add(CardDescription);

            var CardType = new TextBlock
            {
                Name = "cardType",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
                Foreground = Brushes.White,
                Background = typeColor,
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                Width = 133,
                Height = 27
            };
            CardType.Text = (rarity == "LEGENDARY" ? "★ | " : "") + cardTypeText;
            Canvas.SetLeft(CardType, 54);
            Canvas.SetTop(CardType, 0);
            canvasCardDetails.Children.Add(CardType);

            // Add the text blocks to the canvas
            canvasCardDetails.Visibility = Visibility.Visible;
        }
    }

}

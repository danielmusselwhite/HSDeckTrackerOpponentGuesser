using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace HDT_OpponentGuesser
{
    public partial class BestFitDeckDisplay : System.Windows.Controls.UserControl
    {
        private string _deckId = null;
        private double _minimumMatch;
        private DateTime _timeAFterClick = DateTime.Now;
        private List<CardInfo> _guessedDeckList = null;

        public BestFitDeckDisplay()
        {
            InitializeComponent();
            Update(null);
            canvasDeckView.Visibility = Visibility.Hidden;
        }

        public void SetMinimumMatch(double minimumMatch)
        {
            _minimumMatch = minimumMatch;
        }

        public void Update(string deckName, double winRate = -1, double bestFitDeckMatchPercent = -1, string deckId = null, List<CardInfo> guessedDeckList=null)
        {
            _guessedDeckList = guessedDeckList;

            // Used to generate link to deck if user clicks on button
            this._deckId = deckId;

            if (deckName != null && winRate != -1 && bestFitDeckMatchPercent != -1 && deckId != null)
            {
                this.deckNameBlock.Text = deckName + " (" + deckId.Substring(0, 4) + ")"; // including first 4 characters of deckId to differentiate between decks with same name
                this.winRateBlock.Text = ((int)Math.Round((double)winRate)).ToString() + "% WR";
                this.matchPercentBlock.Text = ((int)Math.Round((double)bestFitDeckMatchPercent)).ToString() + "% Match";
                this.viewDeckButton.Visibility = Visibility.Visible;
                UpdateDeckCardViews();
            }
            else
            {
                this.deckNameBlock.Text = "No Matches Above " + _minimumMatch + "%";
                this.winRateBlock.Text = "";
                this.matchPercentBlock.Text = "";
                this.viewDeckButton.Visibility = Visibility.Hidden;
                this.canvasDeckView.Visibility = Visibility.Hidden;
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Canvas.SetBottom(this, 12);
            Canvas.SetLeft(this, 100);
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }

        private void UpdateDeckCardViews()
        {
            #region Populating canvasDeckView with the cards in the deck
            // Destroy all contents from canvasDeckView (if any)
            canvasDeckView.Children.Clear();
            CardView.ResetCounter();

            // Creating a CardView for each card in the deck
            List<CardView> cardViews = new List<CardView>();
            foreach (CardInfo card in _guessedDeckList)
            {

                // check if cardViews already has a card with the same dbfID
                if (cardViews.Any(x => x.GetDbfId() == card.GetDbfId()))
                {
                    // if so, increment the count of that card
                    cardViews.Find(x => x.GetDbfId() == card.GetDbfId()).IncrementCount();

                }// if not, add the card to cardViews
                else
                {
                    cardViews.Add(new CardView(card.GetName(), card.GetCost(), card.GetHealth(), card.GetAttack(), card.GetDescription(), card.GetCardType(), card.GetDbfId(), card.GetRarity(), this.canvasDeckView));
                }
            }

            // Add each cardView to the canvasDeckView
            foreach (CardView cardView in cardViews)
            {
                canvasDeckView.Children.Add(cardView);
            }

            // Make the canvasDeckViews height fit its contents
            canvasDeckView.Height = cardViews.Count * (CardView.height+CardView.spaceBetween)+2;

            #endregion

        }



        //Function to check if the mouse is over the controls various components, and if so view the deck
        public void HandleMouseOver()
        {
            // If hearthstone is in the foreground
            if (User32.IsHearthstoneInForeground())
            {
                // If Mouse is Over the the viewDeckButton or the canvasDeckView, then show the deck
                if (IsMouseOverElement(this.viewDeckButton) || IsMouseOverElement(this.canvasDeckView) || IsMouseOverElement(this.canvasCardDetails))
                {
                    // Highlight the button and show the deckView
                    this.viewDeckButton.Background = Brushes.LightSlateGray;
                    canvasDeckView.Visibility = Visibility.Visible;

                    //detect if player has clicked on the button; first confirm users mouse if over the button
                    if (IsMouseOverElement(this.viewDeckButton) && DateTime.Now > _timeAFterClick)
                    {
                        _timeAFterClick = DateTime.Now.AddSeconds(3);
                        new User32.MouseInput().LmbDown += ViewButtonClicked; // then, if LmbDown event is triggered, call ViewButtonClicked
                    }

                    // Default the card details to hidden
                    canvasCardDetails.Visibility = Visibility.Hidden;
                    // For each CardView in the canvasDeckView, check if the mouse is over it
                    foreach (CardView cardView in canvasDeckView.Children)
                    {

                        if (IsMouseOverElement(cardView))
                        {
                            // If so, show the card details
                            cardView.ShowCardDetails(canvasCardDetails);
                            break;
                        }
                        else
                        {
                            // If not, hide the card details
                            canvasCardDetails.Visibility = Visibility.Hidden;
                        }
                    }

                }
                else
                {
                    this.viewDeckButton.Background = Brushes.SlateBlue;
                    canvasDeckView.Visibility = Visibility.Hidden;
                    canvasCardDetails.Visibility = Visibility.Hidden;
                }
            }
        }

        // Function to check if the mouse is over a specific element
        private bool IsMouseOverElement(FrameworkElement elem)
        {
            if (elem != null && elem.IsVisible)
            {
                var pos = User32.GetMousePos();
                Point relativePos = elem.PointFromScreen(new Point(pos.X, pos.Y));
                return relativePos.X > 0 && relativePos.X < elem.ActualWidth && relativePos.Y > 0 && relativePos.Y < elem.ActualHeight;
            }
            return false;
        }

        private void ViewButtonClicked(object sender, EventArgs eventArgs)
        {
            string url = $"https://hsreplay.net/decks/{_deckId}/#rankRange=GOLD&gameType=RANKED_STANDARD";
            Log.Debug(url);
            System.Diagnostics.Process.Start(url);
        }

        private void UpdateDeckId(string deckId)
        {
            _deckId = deckId;
        }

    }


    // Class to represent cards view in the decklist; taking in on creation: count, name, cost, health, attack, description
    public class CardView : TextBlock
    {
        private string _name ;
        private int _cost;
        private string _health;
        private string _attack;
        private int _count;
        private string _description;
        private string _cardType;
        private int _dbfId;
        private string _rarity;
        public static int cardNumber = 0;
        public static int height = 14;
        public static int spaceBetween = 2;
        Canvas _parent;


        public CardView(string name, int cost, string health, string attack, string description, string type, int dbfId, string rarity, Canvas parent)
        {
            // store the variables
            _name = name;
            _cost = cost;
            _health = health;
            _attack = attack;
            _description = description;
            _count = 1;
            _cardType = type;
            _dbfId = dbfId;
            _parent = parent;
            _rarity = rarity;


            // create a dict of Type:Color, where: Minion:Orange, Spell:Blue, Secret:Magenta, Weapon:Magenta, Location:Yellow
            Dictionary<string, SolidColorBrush> typeColorDict = new Dictionary<string, SolidColorBrush>();
            typeColorDict.Add("MINION", Brushes.Maroon);
            typeColorDict.Add("SPELL", Brushes.DarkBlue);
            typeColorDict.Add("SECRET", Brushes.DarkMagenta);
            typeColorDict.Add("WEAPON", Brushes.DarkGoldenrod);
            typeColorDict.Add("LOCATION", Brushes.DarkGreen);

            // create a new textblock to display the cost, name, and count inside of canvasDeckView
            this.Name = "deckCard" + cardNumber;
            this.Text = UpdateText();
            this.FontSize = 12;
            this.Foreground = Brushes.White;
            this.Background = typeColorDict[type];
            this.FontWeight = FontWeights.Bold;
            this.Height = height;
            this.Width = 280;
            this.TextAlignment = TextAlignment.Left;
            this.TextWrapping = TextWrapping.Wrap;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.Margin = new Thickness(0, 0, 0, 0);
            this.Padding = new Thickness(0, 0, 0, 0);

            // setting the Vertical position of the cards in order based on cardNumber
            this.SetValue(Canvas.BottomProperty, (double) cardNumber * (height+spaceBetween) + spaceBetween);
            this.SetValue(Canvas.LeftProperty, (double) (_parent.Width - this.Width) / 2);


            cardNumber++;

        }

        public static void ResetCounter()
        {
            cardNumber = 0;
        }

        public int GetDbfId()
        {
            return _dbfId;
        }

        public void IncrementCount()
        {
            _count++;
            this.Text = UpdateText();
        }

        private string UpdateText()
        {
            // ternary to also add a star symbol if rarity is "LEGENDARY"
            return _count+"x   |   " + _cost + " Mana   |   " + _name + (_rarity == "LEGENDARY" ? " ★" : "");
        }

        public void ShowCardDetails(Canvas canvas)
        {
            // clear the canvas
            canvas.Children.Clear();

            CardDetailsCanvasPopulator.populateCardDetails(canvas, _name, _cost, _health, _attack, _description, _cardType, _rarity, this.Background);
        }
    }
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
            CardMana.Text = cardManaValue.ToString()+ " 💎";
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
                CardHealth.Text = cardHealthText+ " ♡";
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
                CardAttack.Text = cardAttackText+ " ⚔";
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
            cardDescriptionText = cardDescriptionText.Replace("@", "").Replace("$", "").Replace("{","").Replace("}","");
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

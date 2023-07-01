using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace HDT_OpponentGuesser
{
    public partial class BestFitDeckDisplay : System.Windows.Controls.UserControl
    {
        private string _deckId = null;
        private double _minimumMatch;
        private DateTime _timeAFterClick = DateTime.Now;
        private List<CardInfo> _guessedDeckList = null;
        private bool alreadyHovering = false;

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
            Log.Info("Called UpdateDeckCardViews()");


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
                }
                else
                {
                    Log.Info("Creating Card View For the Values of: " + card.GetName() + " " + card.GetCost() + " " + card.GetHealth() + " " + card.GetAttack() + " " + card.GetDescription() + " " + card.GetCardType() + " " + card.GetDbfId() + "");
                    // if not, add the card to cardViews
                    cardViews.Add(new CardView(card.GetName(), card.GetCost(), card.GetHealth(), card.GetAttack(), card.GetDescription(), card.GetCardType(), card.GetDbfId(), this.canvasDeckView));
                }
            }

            // Add each cardView to the canvasDeckView
            foreach (CardView cardView in cardViews)
            {
                canvasDeckView.Children.Add(cardView);
            }
            #endregion

        }



        //Function to check if the mouse is over the controls various components, and if so view the deck
        public void HandleMouseOver()
        {
            // If hearthstone is in the foreground
            if (User32.IsHearthstoneInForeground())
            {
                // If Mouse is Over the the viewDeckButton or the canvasDeckView, then show the deck
                if (IsMouseOverElement(this.viewDeckButton) || IsMouseOverElement(this.canvasDeckView))
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

                }
                else
                {
                    this.viewDeckButton.Background = Brushes.SlateBlue;
                    canvasDeckView.Visibility = Visibility.Hidden;
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


            // TODO - find a way to show the deck
            // Either see how they're doing it in decktracker already to replicate
            // Or can simply do something eg make a new class called "DeckView" and "CardView" then create a CardView for each Card in the deck inside DeckView and show/hide that based on this
        }

    }


    // Class to represent cards view in the decklist; taking in on creation: count, name, cost, health, attack, description
    public class CardView : TextBlock
    {
        private string _name;
        private int _cost;
        private string _health;
        private string _attack;
        private int _count;
        private string _description;
        private string _cardType;
        private int _dbfId;
        public static int cardNumber = 0;
        public static int height = 14;
        Canvas _parent;


        public CardView(string name, int cost, string health, string attack, string description, string type, int dbfId, Canvas parent)
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


            // create a dict of Type:Color, where: Minion:Orange, Spell:Cyan, Secret:Blue, Weapon:Magenta, Location:Yellow
            Dictionary<string, SolidColorBrush> typeColorDict = new Dictionary<string, SolidColorBrush>();
            typeColorDict.Add("MINION", Brushes.Maroon);
            typeColorDict.Add("SPELL", Brushes.DarkCyan);
            typeColorDict.Add("SECRET", Brushes.DarkBlue);
            typeColorDict.Add("WEAPON", Brushes.DarkMagenta);
            typeColorDict.Add("LOCATION", Brushes.DarkGoldenrod);

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
            this.SetValue(Canvas.BottomProperty, (double) cardNumber * (height+2));
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
            return _count+"x   |   " + _cost + " Mana   |   " + _name;
        }
    }
}

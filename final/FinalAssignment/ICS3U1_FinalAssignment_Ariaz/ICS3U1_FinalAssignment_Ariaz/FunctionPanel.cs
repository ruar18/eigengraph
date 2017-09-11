/*
 * Function Panel class
 * June 31st, 2017 - Rudy Ariaz
 * Displays function-selection panels, adds controls to allow the user to manipulate the function, 
 * and sends messages to the main class through button presses and other events.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ICS3U1_FinalAssignment_Ariaz
{
    class FunctionPanel
    {
        // Object that stores the current function selected
        private Function thisFunction;
        // Object that stores the current Panel
        private Panel thisPnl;
        // Labels for the function selection buttons
        private static readonly string[] buttonLabels = { "Power", "Trig", "Exp", "Log" };
        private static readonly string[] sliderLabels = { "a", "b", "h", "k" };
        // Booleans to keep track of whether the drop down menu is already activated for this panel,
        // and whether the integration button has been already displayed 
        private bool isDropDown, isIntegrate;
        // The drop-down menu that allows the user to select specific types of functions
        private ComboBox dropDown;
        // The general type of function selected through the buttons,
        // and the specific type of function selected through the drop-down menu
        private string buttonSelected, itemSelected;
        // String that notifies the user about an error
        private const string ERROR_PROMPT = "Invalid: select function from drop-down menu";
        // Label that shows information to the user about the function selected
        Label output;
        // Dictionary that associates parameter-selection sliders to their respective transformation names
        private Dictionary<string, TrackBar> transformations;
        // Public events regarding button presses that the main form class can subscribe to
        public event EventHandler AddPressed, IntegratePressed;

        // Constructor
        public FunctionPanel()
        {
            // Instantiate the output label, change its font
            output = new Label();
            output.Font = new Font("Segoe UI", 10);
            // Make sure the label can resize automatically
            output.AutoSize = true;
            // Instantiate the current panel to a panel that can adjust its layout automatically
            thisPnl = new FlowLayoutPanel();
            // Set the size, location and colour of the panel
            thisPnl.Size = new Size(165, 500);
            thisPnl.Location = new Point(50, 50);
            thisPnl.BackColor = Color.Gray;
            // Draws buttons for selecting function categories
            foreach (int i in Enumerable.Range(0, 4))
            {
                // Declare and instantiate a new button
                Button functionType = new Button();
                // Set the text, colour and name of the button
                functionType.Text = buttonLabels[i];
                functionType.BackColor = Color.Azure;
                functionType.Name = String.Format("btn{0}", buttonLabels[i]);
                // Associate the button click event with one event handler that will handle all button clicks
                functionType.Click += new EventHandler(ChooseButton_Click);
                // Add the button to the panel
                thisPnl.Controls.Add(functionType);
            }
        }

        // Function-selection button click event handler
        private void ChooseButton_Click(object sender, EventArgs e)
        {
            // Cast the object that activated the event to a button
            Button thisBtn = sender as Button;
            // Get the text of the button to know the type of function selected
            buttonSelected = thisBtn.Text;
            // Change current drop-down menu if it exists
            if (isDropDown)
            {
                // Go through each control in the current panel to find the current drop-down menu
                // Not very efficient, but very reliable as only one combobox will be on the panel at all times
                foreach (Control c in thisPnl.Controls)
                {
                    // If the current control is a combobox (drop-down menu):
                    if (c.GetType() == typeof(ComboBox))
                    {
                        // Cast the control to a combobox
                        ComboBox comb = c as ComboBox;
                        // Clear the current items 
                        comb.Items.Clear();
                        // Unless the function is a power function, make the drop-down menu read only
                        comb.DropDownStyle =
                            buttonSelected == "Power" ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
                        // Add updated relevant items to the menu by calling AddDropDownItems
                        AddDropDownItems(thisBtn, comb);
                        // Exit the method
                        return;
                    }
                }
            }
            // Add new combobox
            dropDown = new ComboBox();
            // Unless the function is a power function, make the drop-down menu read only
            if (buttonSelected != "Power")
                dropDown.DropDownStyle = ComboBoxStyle.DropDownList;
            // Add items to the combobox by calling AddDropDownItems
            AddDropDownItems(thisBtn, dropDown);
            // Add the drop-down menu to the panel
            thisPnl.Controls.Add(dropDown);
            // Record that a drop-down menu has already been updated
            isDropDown = true;
            // Instantiate the transformations dictionary
            transformations = new Dictionary<string, TrackBar>();
            // Add sliders to change function parameters (a, b, h and k transformations)
            foreach (string label in sliderLabels)
            {
                // Instantiate a new slider
                TrackBar currentSlider = new TrackBar();
                // Set the slider's minimum and maximum values, as well as its tag
                currentSlider.Minimum = -3;
                currentSlider.Maximum = 3;
                currentSlider.Tag = label;
                // Default value is 0. If the slider corresponds to a multiplicative transformation,
                // set the slider's value to 1, the multiplicative identity
                if (label == "a" || label == "b")
                    currentSlider.Value = 1;
                // Instantiate a label to display the slider's transformation's name
                Label sliderLabel = new Label();
                // Change its font and text
                sliderLabel.Font = new Font("Segoe UI", 10);
                sliderLabel.Text = label;
                // Add the label and the slider to the panel
                thisPnl.Controls.Add(sliderLabel);
                thisPnl.Controls.Add(currentSlider);
                // Add the slider to the transformations dictionary, with its label as the key
                transformations[label] = currentSlider;
            }
            // Add "add" button to add the current function
            Button addButton = new Button();
            addButton.Text = "Done";
            addButton.BackColor = Color.Azure;
            // Associate the AddButton event handler with the button click
            addButton.Click += new EventHandler(AddButton_Click);
            // Add the button and the label to the panel
            thisPnl.Controls.Add(addButton);
            thisPnl.Controls.Add(output);
        }

        // Event handler for the press of the "Add" button, which draws the currently selected function
        private void AddButton_Click(object sender, EventArgs e)
        {
            // Get the drop-down item selected
            itemSelected = dropDown.Text;
            // If a real function type was selected from the drop-down menu:
            if (itemSelected != "Select Function")
            {
                // Display the mapping from an input to the result of applying the current function to the input
                // in the output label, using the GetTransformations function
                output.Text = GetTransformations();
                // Update the current function
                UpdateFunction();
            }

            // Otherwise, alert the user that they must select a function from the drop-down menu
            else
                output.Text = ERROR_PROMPT;

            // Invoke the AddPressed event to notify the main form class to draw the function
            // "this" corresponds to the sender of the event, "null" to additional arguments of the event
            // Use the null-conditional operator (?.) to make sure that no null reference exception is thrown
            AddPressed?.Invoke(this, null);
            // If an integration button has not been added yet, add it
            if (!isIntegrate)
            {
                // Instantiate a new button
                Button integrateButton = new Button();
                // Adjust its text and colour
                integrateButton.Text = "Integrate";
                integrateButton.BackColor = Color.Azure;
                // Associate the press of the button with the IntegrateButton event handler
                integrateButton.Click += new EventHandler(IntegrateButton_Click);
                // Add the button to the panel
                thisPnl.Controls.Add(integrateButton);
                // Record that an integration button has been added
                isIntegrate = true;
            }
        }

        // Event handler for the press of the integration button
        private void IntegrateButton_Click(object sender, EventArgs e)
        {
            // Invoke the IntegratePressed event to notify the main form class to draw the integral
            // "this" corresponds to the sender of the event, "null" to additional arguments of the event
            // Use the null-conditional operator (?.) to make sure that no null reference exception is thrown
            IntegratePressed?.Invoke(this, null);
        }

        /// <summary>
        /// Adds items to the drop-down menu
        /// </summary>
        /// <param name="thisBtn"> button on which items will be based </param>
        /// <param name="dropDown"> drop-down menu to which items will be added </param>
        private void AddDropDownItems(Button thisBtn, ComboBox dropDown)
        {
            // Insert the default selection prompt at the top of the drop-down menu
            dropDown.Items.Insert(0, "Select Function");
            // If the button pressed was to select a power function:
            if (thisBtn.Text == "Power")
            {
                // Add the function f(x)=x
                dropDown.Items.Add("x");
                // Add power functions from quadratic up to quintic
                for (int i = 2; i <= 5; i++)
                    dropDown.Items.Add("x^" + i);
            }
            // If the button pressed was to select a trigonometric function:
            else if (thisBtn.Text == "Trig")
            {
                // Add the three primary trigonometric functions
                dropDown.Items.Add("sin x");
                dropDown.Items.Add("cos x");
                dropDown.Items.Add("tan x");
            }
            // If the button pressed was to select an exponential function:
            else if (thisBtn.Text == "Exp")
            {
                // Add exponential functions with bases 2 through 5
                for (int i = 2; i <= 5; i++)
                    dropDown.Items.Add(i + "^x");
            }
            // If the button selected was a logarithmic function:
            else
            {
                // Add the three most common logarithmic functions
                dropDown.Items.Add("log x");
                dropDown.Items.Add("lg x");
                dropDown.Items.Add("ln x");
            }
            // Make sure the initially-selected item is the first one (selection prompt item)
            dropDown.SelectedIndex = 0;
        }

        // Get the current panel object
        public Panel GetPanel() { return thisPnl; }

        // Get a string description of the mapping from an input x to the point (x,y) after
        // the current function was applied to x
        public string GetTransformations()
        {
            // Store the transformations applied to the function in a, b, h and k
            int a = transformations["a"].Value, b = transformations["b"].Value, h = transformations["h"].Value,
                k = transformations["k"].Value;
            // Declare variables that will store the transformations in string form
            string aText, bText = "", hText, kText;
            // Determine special cases
            // 1 is the multiplicative identity, so if a == 1, we do not need any text to represent a
            if (a == 1)
                aText = "";
            // If a == -1, we can simply represent the transformation with a negative sign
            else if (a == -1)
                aText = "-";
            // Otherwise, convert the transformation a to a string and store it
            else
                aText = a.ToString();
            // 1 is the multiplicative identity, so if b == 1, we do not need any text to represent b
            if (b == 1)
                bText = "";
            // If b == -1, we can simply represent the transformation with a negative sign
            else if (b == -1)
                bText = "-";
            // 0 is the additive identity, so if h == 1, we do not need any text to represent h
            if (h == 0)
                hText = "";
            // If h is positive, we will need an addition sign in front of the transformation
            else if (h > 0)
                hText = " + " + h;
            // If h is negative, h is already represented with a negative sign (as an int),
            // and so we do not need a subtraction sign in front of it
            else
                hText = " " + h;
            // 0 is the additive identity, so if k == 1, we do not need any text to represent k
            if (k == 0)
                kText = "";
            // If k is positive, we will need an addition sign in front of the transformation
            else if (k > 0)
                kText = " + " + k;
            // If k is negative, k is already represented with a negative sign (as an int),
            // and so we do not need a subtraction sign in front of it
            else
                kText = " " + k;

            // Begin the mapping representation, storing it in the variable output
            // Represent mapping with a -> sign
            string output = "x -> (x,";
            // Store the base function as a string
            string baseFunc = "(" + itemSelected + ")";
            // Add the base function to the output string, representing the first step in the mapping
            // The mapping from the input x to the point (x, f(x)) where f is the base function
            output += baseFunc + ") -> \r\n(";
            // If b is 0, the function is a horizontal line with the equation y = k, so add this to the output
            if (b == 0)
                output += String.Format("x, {0})", k);
            // Otherwise, continue the mapping normally
            else
            {
                // If b is not 1 or -1 (which are special cases), the new input to the transformed function
                // is proportional to 1/b, so add the text (1/b) to the output (it will be multiplied by x-h)
                if (b != 1 && b != -1)
                {
                    bText = "(1/" + b + ")";

                }
                // Add all the transformations which are applied to x
                // (1/b)(x-h)
                output += bText + "x" + hText + ", ";
                // If a is 0, the function is a horizontal line with the equation y = k, so add this to the input
                if (a == 0)
                    output += kText + ")";
                // Otherwise, add the vertical stretch to the mapping (a) multiplied by the original output of the 
                // base function, and added to the vertical shift (k)
                else
                    output += aText + baseFunc + kText + ")";
            }
            // Return the mapping representation
            return output;
        }

        // Updates the current function based on the state of the panel
        private void UpdateFunction()
        {
            // Store the transformations applied to the function in a, b, h and k
            int a = transformations["a"].Value, b = transformations["b"].Value, h = transformations["h"].Value,
                k = transformations["k"].Value;
            // If the base function is a power function:
            if (buttonSelected == "Power")
            {
                // If the base function is f(x) = x, change the function to represent this
                if (itemSelected == "x")
                    thisFunction = new Function(a, b, h, k, "Power", 1);
                // Otherwise, get the exponent of the function using string manipulation and change the function
                else
                    thisFunction = new Function(a, b, h, k, "Power", Convert.ToInt32(itemSelected.Substring(2)));
            }
            // If the base function is a trigonometric function:
            else if (buttonSelected == "Trig")
                // Use string manipulation to get the specific type of the function and change the function
                thisFunction = new Function(a, b, h, k, "Trig", itemSelected.Substring(0, 3));
            // If the base function is an exponential function:
            else if (buttonSelected == "Exp")
                // Use string manipulation to get the base of the function and change the function accordingly
                thisFunction = new Function(a, b, h, k, "Exp", Convert.ToInt32(itemSelected.Substring(0, 1)));
            // If the base function is a logarithmic function:
            else
            {
                // If the base function is f(x) = log x (which has a different number of characters
                // from the other logarithmic functions), change the function accordingly
                if (itemSelected == "log x")
                    thisFunction = new Function(a, b, h, k, "Log", "log");
                // Otherwise, use string manipulation to get the type of function and change the function
                else
                    thisFunction = new Function(a, b, h, k, "Log", itemSelected.Substring(0, 2));
            }
        }

        // Get the current function selected through the panel
        public Function GetFunction() { return thisFunction; }
    }
}

/*
 * EigenGraph - Simple and fast graphing software
 * June 31st, 2017 - Rudy Ariaz
 * A graphing software focused on usability and consistency. Allows for plotting of simple functions and
 * visualizing their integrals. Enables students to explore the effects of transformations on basic functions.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ICS3U1_FinalAssignment_Ariaz
{
    public partial class Form1 : Form
    {
        // The origin of the graph: all other coordinates are graphed based on the origin,
        // so that it is simple to modify location of graphs by just shifting the origin
        private PointF origin = new PointF(600, 300);
        // Length of each axis (from origin) (only half of the entire axis)
        private const int AXIS_LENGTH = 250;
        // Scale factors to make functions clearer and more spaced-out
        private const int POWER_SCALE_FACTOR = 20, TRIG_SCALE_FACTOR = 5, EXP_SCALE_FACTOR = 10,
            LOG_SCALE_FACTOR = 20;
        // A margin around the set of axes
        private const int PLANE_MARGIN = 25;
        // The function being plotted
        private Function plottedFunction;
        // The function selection panel object
        FunctionPanel selectionPanel;
        // Epsilon used to graph logarithmic functions and to integrate
        private const double EPSILON = 0.001;
        // Booleans to keep track of whether the instructions should be displayed, 
        // and whether the integration button was pressed
        private bool instructionsOn, isIntegrate;
        // Font to display the instructions
        private Font instructionsFont = new Font("Segoe UI", 12);
        // Dictionary that associates function names (of trigonometric and logarithmic functions)
        // with their corresponding functions, to be used in generating plot points efficiently
        private readonly Dictionary<string, Func<double, double>> specialFuncs =
            new Dictionary<string, Func<double, double>>
            {
                { "sin", Math.Sin },
                { "cos", Math.Cos },
                { "tan", Math.Tan },
                { "log", Math.Log10 },
                { "lg", x => Math.Log(x,2) },
                { "ln", Math.Log }
            };
        // Dictionary that assosciates general function types with their corresponding colours
        // which are used to graph the functions
        private readonly Dictionary<string, Pen> functionColours = new Dictionary<string, Pen>
        {
            { "Power", Pens.Blue },
            { "Trig", Pens.Red },
            { "Exp", Pens.Green },
            { "Log", Pens.Magenta }
        };

        // Where program execution begins
        public Form1()
        {
            InitializeComponent();
            // Maximize the window
            WindowState = FormWindowState.Maximized;
            // Initially, the instructions should be displayed
            instructionsOn = true;
        }

        // Adds a new panel to the form
        private void AddPanel()
        {
            // Instantiate a new FunctionPanel
            selectionPanel = new FunctionPanel();
            // Associate button click events with event handlers
            selectionPanel.AddPressed += AddPressedHandler;
            selectionPanel.IntegratePressed += IntegratePressedHandler;
            // Add the panel to the form
            Panel thisPanel = selectionPanel.GetPanel();
            Controls.Add(thisPanel);
        }

        // Handles the press of the integration button
        public void IntegratePressedHandler(object sender, EventArgs e)
        {
            // When the button is pressed, store this information in a boolean variable
            isIntegrate = true;
            // Redraw the graphics (to draw the integration animation)
            Refresh();
        }

        // Drawing and plotting of graphs happens here
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // If the instructions should be displayed:
            if (instructionsOn)
            {
                // Set a background image
                e.Graphics.DrawImage(Properties.Resources.instructionsBackground, ClientRectangle);
                // Draw a mathematical image and its description
                e.Graphics.DrawImage(Properties.Resources.bubble, 1000, 50, 200, 200);
                e.Graphics.DrawString(Properties.Resources.bubbleDesc, new Font("Calibri", 10),
                    Brushes.Black, 975, 275);
                // Draw the instructions in a black font
                e.Graphics.DrawString(Properties.Resources.instructions, instructionsFont,
                                    Brushes.Black, 50, 50);
                // Do not draw anything else on the screen yet
                return;
            }
            // Set a different background image for the main screen
            e.Graphics.DrawImage(Properties.Resources.background, ClientRectangle);
            // Draw a square around the set of axes to make functions clearer against the background
            e.Graphics.FillRectangle(Brushes.LightGray, origin.X - AXIS_LENGTH - PLANE_MARGIN, origin.Y - AXIS_LENGTH - PLANE_MARGIN,
                2 * (AXIS_LENGTH + PLANE_MARGIN), 2 * (AXIS_LENGTH + PLANE_MARGIN));
            // Draw y-axis
            e.Graphics.DrawLine(Pens.Black, new PointF(origin.X, origin.Y - AXIS_LENGTH),
                new PointF(origin.X, origin.Y + AXIS_LENGTH));
            // Draw x-axis
            e.Graphics.DrawLine(Pens.Black, new PointF(origin.X - AXIS_LENGTH, origin.Y),
                new PointF(origin.X + AXIS_LENGTH, origin.Y));
            // Create an array of points to plot
            PointF[] plots;
            // If the function to be graphed has been selected:
            if (plottedFunction != null)
                // Call the Plot function to generate a list of points,
                // convert the list into an array and save it in variable plots
                plots = Plot(plottedFunction)?.ToArray();
            // Otherwise, don't plot anything since a null reference exception will be thrown
            else
                return;
            // Get the type of the function being plotted
            string thisType = plottedFunction.GetDesc();
            // Draw a curve with the colour corresponding to the function, through the points
            // calculated by calling the Plot function
            e.Graphics.DrawCurve(functionColours[plottedFunction.GetDesc()], plots, 0.5f);
            // If the integration button was pressed:
            if (isIntegrate)
            {
                // Draw the integration visualization by calling the Integrate function
                e.Graphics.DrawRectangles(Pens.Orange, Integrate(plots.ToList()).ToArray());
                // Record that the integration animation has happened, and should not happen again
                // if a function is plotted unless the integration button is repressed
                isIntegrate = false;
            }
        }

        /// <summary>
        /// Returns approximation of the visualization of the integral of a given function.
        /// The integral is definite, with lower bound being the leftmost point on the graph,
        /// and the upper bound being the rightmost point on the graph.
        /// </summary>
        /// <param name="points"> some points that are on the graph of the integrated function param>
        /// <returns> a list of rectangles that approximate the integral of the function </returns>
        private List<RectangleF> Integrate(List<PointF> points)
        {
            // List of approximating rectangles
            List<RectangleF> approx = new List<RectangleF>();
            // dx is the "small step" taken to the right when integrating, so it is the width of every rectangle
            const float dx = 0.2f;
            // Loop through each point in the list of points that are on the integrated function
            foreach (PointF graphPoint in points)
            {
                // Current rectangle
                RectangleF cur;
                // If the x-axis is practically tangent to the graphed function, do not integrate here
                if (graphPoint.Y <= origin.Y + EPSILON && graphPoint.Y >= origin.Y - EPSILON)
                    continue;
                // If the graphed function practically passes through the y-axis, do not integrate here
                if (graphPoint.X <= origin.X + 1.2 && graphPoint.X >= origin.X - 1.2)
                    continue;
                // If the graph is above the x-axis:
                else if (graphPoint.Y < origin.Y)
                    // Set cur to a rectangle that spans the area from the graph down to the x-axis
                    // Some adjustments are made so that the rectangle does not hide the graph/x-axis
                    cur = new RectangleF(graphPoint.X, graphPoint.Y + 1, dx, Math.Abs(graphPoint.Y - origin.Y) - 2);
                // If the graph is below the x-axis:
                else
                    // Set cur to a rectangle that spans the area from the graph up to the x-axis
                    // Some adjustments are made so that the rectangle does not hide the graph/x-axis
                    cur = new RectangleF(new PointF(graphPoint.X, origin.Y + 1), new SizeF(dx, graphPoint.Y - origin.Y - 2));
                // Add the calculated rectangle to the list of approximating rectangles
                approx.Add(cur);
            }
            // Return the list of approximations
            return approx;
        }

        // Handles the press of the "Add" button, to draw graphs
        public void AddPressedHandler(object sender, EventArgs e)
        {
            // Set the plotted function to the function selected in the panel
            plottedFunction = selectionPanel.GetFunction();
            // Draw graphics again by calling Refresh()
            Refresh();
        }

        // Event handler for the start button click
        private void btnStart_Click(object sender, EventArgs e)
        {
            // Hide the instructions and the start button
            instructionsOn = false;
            btnStart.Hide();
            Refresh();
            // Add a function-selection panel
            AddPanel();
        }

        // Generate a list of points to plot (of the input function)
        private List<PointF> Plot(Function func)
        {
            // Get and store the transformations applied to the given base function
            var trans = func.GetTransforms();
            // Amplify the horizontal and vertical shifts to make them more noticeable 
            trans["h"] *= 15;
            trans["k"] *= 15;
            // Instantiate the list of points
            List<PointF> points = new List<PointF>();
            // Store the base type of the function (Power, Trigonometric, Exponential, Logarithmic)
            string type = func.GetDesc();
            // If the function is a power function:
            if (type == "Power")
                // Loop from the left side of the plane to the right side
                for (double i = -AXIS_LENGTH * POWER_SCALE_FACTOR; i <= AXIS_LENGTH * POWER_SCALE_FACTOR; i += 0.5)
                    // Generate a point and add it to the points list using the AddPoint function,
                    // passing in a lambda that represents raising the loop variable to the selected exponent
                    AddPoint(i, POWER_SCALE_FACTOR, trans, x => Math.Pow(x, func.GetAdditional()), ref points);
            // If the function is a trigonometric function:
            else if (type == "Trig")
                // Loop from the left side of the plane to the right side
                for (double i = -AXIS_LENGTH * TRIG_SCALE_FACTOR; i <= AXIS_LENGTH * TRIG_SCALE_FACTOR; i += 0.5)
                    // Generate a point and add it to the points list using the AddPoint function,
                    // passing in the trigonometric function that corresponds to the selected function
                    AddPoint(i, TRIG_SCALE_FACTOR, trans, specialFuncs[func.GetSpecialType()], ref points);
            // If the function is an exponential function:
            else if (type == "Exp")
                // Loop from the left side of the plane to the right side
                for (double i = -AXIS_LENGTH * EXP_SCALE_FACTOR; i <= AXIS_LENGTH * EXP_SCALE_FACTOR; i += 0.5)
                    // Generate a point and add it to the points list using the AddPoint function,
                    // passing in a lambda that represents raising the selected base to the ith exponent
                    AddPoint(i, EXP_SCALE_FACTOR, trans, x => Math.Pow(func.GetAdditional(), x), ref points);
            // If the function is a logarithmic function:
            else
                // Loop from an epsilon to the right of the y-axis to the right side of the plane
                for (double i = EPSILON * LOG_SCALE_FACTOR; i <= AXIS_LENGTH * LOG_SCALE_FACTOR; i += 0.5)
                    // Generate a point and add it to the points list using the AddPoint function,
                    // passing in the logarithmic function that corresponds to the selected function
                    AddPoint(i, TRIG_SCALE_FACTOR, trans, specialFuncs[func.GetSpecialType()], ref points);
            // Return the list of points to be plotted
            return points;
        }

        /// <summary>
        /// Computes the mapping of x->(x,y) based on the function and transformations
        /// This is the function that does the hardest computation 
        /// </summary>
        /// <param name="a"> vertical stretch factor </param>
        /// <param name="b"> horizontal stretch factor </param>
        /// <param name="h"> horizontal shift </param>
        /// <param name="k"> vertical shift </param>
        /// <param name="x"> input to the function </param>
        /// <param name="y"> output of the function </param>
        /// <param name="scaleFactor"> scale factor that corresponds to the given function </param>
        /// <param name="f"> the function applied to the input </param>
        /// <returns> a tuple with the new x and y coordinates, mapped from the original input x </returns>
        private Tuple<double, double> Transform(double a, double b, double h, double k, double x, double y,
            double scaleFactor, Func<double, double> f)
        {
            // Mapping is done in steps to make computation clearer 
            // Apply the function to the input, scaled by the special scale factor. We do not yet multiply by b
            // so that we do not accidentally apply a logarithmic function to a negative input
            y = f(x / scaleFactor);
            // Now multiply the input by the horizontal stretch factor
            x *= b;
            // Multiply this output by the vertical stretch and the special stretch factors
            y *= a * scaleFactor;
            // Add the vertical shift to the output
            y += k;
            // Since the plane's top left corner is (0,0), the output must be inverted (reflected in the x-axis)
            y = origin.Y - y;
            // Shift the x coordinate onto the plane, and add to it the horizontal shift
            x += origin.X + h;
            // Return the tuple (x,y)
            return Tuple.Create(x, y);
        }

        /// <summary>
        /// Adds a computed point to a list of points, passed by reference
        /// </summary>
        /// <param name="i"> the input to the function </param>
        /// <param name="scaleFactor"> scale factor that corresponds to the given function </param>
        /// <param name="trans"> dictionary of transformation factors </param>
        /// <param name="f"> function to be applied to the input </param>
        /// <param name="points"> list of points, passed by reference </param>
        private void AddPoint(double i, double scaleFactor, Dictionary<string, int> trans, Func<double, double> f,
            ref List<PointF> points)
        {
            // Initialize the coordinates of the points
            double x = i, y = 0;
            // Apply the Transform function to the input and the transformations
            // to calculate a tuple of the correct, final (x,y) coordinates
            Tuple<double, double> transformed = Transform(trans["a"], trans["b"], trans["h"], trans["k"],
                                                          x, y, 2 * scaleFactor, f);
            // Set each coordinate to its corresponding item in the calculated tuple
            x = transformed.Item1;
            y = transformed.Item2;
            // If the point is out of bounds, return without doing anything
            if (OutOfBounds(x, y))
                return;
            // Otherwise, add the new point to the points list
            points.Add(new PointF((float)x, (float)y));
        }

        // Checks if any of the input coordinates is out of bounds (or is an invalid number)
        private bool OutOfBounds(double x, double y)
        {
            // Return the appropriate boolean value based on checking the boundaries
            return (y < origin.Y - AXIS_LENGTH || y > origin.Y + AXIS_LENGTH ||
                    x < origin.X - AXIS_LENGTH || x > origin.X + AXIS_LENGTH) || x == double.NaN || y == double.NaN;
        }
    }
}

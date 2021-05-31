# Running a machine learning model from Blazor WASM

*An example of the ML workflow on a time prediction analysis, and presentation in a Blazor WASM application.*

ML is a software development paradigm that requires not only knowledge in advanced programming, but also in mathematics, data analysis, and logical thinking.

In this post I am going to show how to solve an interesting machine learning problem based on data published in the community [kaggle](https://www.kaggle.com), about the price of crude oil shares, where we estimate a closing price prediction model. The results are presented on a Blazor WASM front panel.

A Blazor WebAssembly application does not directly run the ML.NET library. Which is somewhat natural in terms of design and threads flow. However, we solve it through a Web API. Subject of which this article deals.

> Let's keep in mind that if our goal covers running a Blazor-based WASM application offline, we don't have the option of using an API service. To cover this case, it is ideal that in the near future that part of the ML.NET library responsible for creating the prediction engine be enabled, so that the functionality can be installed within a Blazor WASM application.

## Application Architecture

As we know, machine learning consists of working on three scenarios: Training, Prediction, and Data. In general, we base the architecture of the application on this conceptual frame.

### Training

The training covers the ML workflow consisting of:

- Data exploration
- Data upload
- Transformations and algorithms
- Creation of the model
- Validation
- Publication

The detail of each of these steps is part of the general theory of ML. In particular, the Behavior and Algorithm Channeling is the job of the data scientist, and it is here that expertise and analytical skills make a difference and make ML an art. The other points are certainly routine. We take into account that the training sequence can be repeated until the validation exceeds a qualitative threshold or, in other words, is reliable.

### Predictor

The predictor is the front-end application that the trained model consumes. A Blazor WASM application hosted on ASP.NET Core is ideal. The server part is in charge of encapsulating the ML.NET functionality, and supplying the API services, which provide data and calculations to the client.

### Data

The dataset is obtained from a post in the kaggle community about the price of crude oil shares, [source](https://www.kaggle.com/awadhi123/crude-oil-stock-price).

> Kaggle, a subsidiary of Google LLC, is an online community of data scientists and machine learning professionals.

In the data treatment with ML.NET, two structures are required, one that represents the data source, and the other that represents the objective of the problem. The first is used by training and the second by predictions. In the Blazor solution, the data classes are written to the shared library.

**The StockPrice class**

It corresponds to the map of the data matrix, together with the decorators required by ML.NET. In the same class I included the predictive class.

```csharp
using Microsoft.ML.Data;

namespace CrudeOilStockPrice.Shared
{
    public class StockPrice
    {
        [LoadColumn(0)] public string Date { get; set; }
        [LoadColumn(1)] public float Open { get; set; }
        [LoadColumn(2)] public float High { get; set; }
        [LoadColumn(3)] public float Low { get; set; }
        [LoadColumn(5)] public float Close { get; set; }
    }

    public class StockPricePrediction : StockPrice
    {
        public float Score { get; set; }
        public int Year => int.Parse(Date[0..4]);
    }
}
```

*Note that, in the class, `StockPricePrediction`, I have inherited the class, `StockPrice`. The reason is that I am going to present all the data on the front, and I want the other data to be available for reading when calculating the predictions. Likewise, the `Year` attribute was added with the purpose of generating a graph where we can filter by year; from this mamera it is efficient to have this attribute, without the need to execute internal operations with an unfair redundant calculation; It is notable that it improves the performance in the functional code of `Linq` when executing the filters.*

## Solution and Projects

The C # solution consists of 4 projects. Three for the hosted Blazor WebAssembly app, and one Console app. Respectively I have created a solution named `BlazorMLCrudeOilPrice`,  and added to this two projects, hosted Blazor: `CrudeOilStockPrice` and the console: `TrainerConsole`.

The solucon is certainly advanced and features plenty of detail and sophistication. I will cover specifically the ML issue, and comment on some particular details.

## Trainer

It consists of the Console application. Requires the `Microsoft.ML` and `Microsoft.ML.FastTree` library. Later I will give details of the inclusion of this last library.

```csharp
using Microsoft.ML;
using System;
using System.Linq;
using CrudeOilStockPrice.Shared;
using System.Threading.Tasks;
using System.Net.Http;
//
using static System.Console;

namespace TrainerConsole
{
    class Program
    {
        static readonly string
            TRAIN_DATA = Utils.DataPath("crudeoil_price-raw.csv"),
            MODEL_FILE = Utils.PublishPath("crudeoil-price-model.zip");

        static void Main()
        {
            WriteLine("Crude Oil Stock Price Model Trainer");

            // data exloration previous works
            var dataFile = Utils.FilterNoiseLines(TRAIN_DATA);

            Train(dataFile);
        }

        static void Train(string dataFile)
        {
            MLContext mlContext = new(seed: 0);

            var dataView = mlContext.Data.LoadFromTextFile<StockPrice>(dataFile, hasHeader: true, separatorChar: ',');

            Utils.LogDataView(dataView, "Data File");

            WriteLine("\nMaking Transforms");

            // transform data
            var pipeline = mlContext.Transforms
                // the output of the model
                .CopyColumns(outputColumnName: "Label", inputColumnName: "Close")

                // transforms to numeric features
                .Append(mlContext.Transforms.Text.FeaturizeText("DateNumber", "Date"))

                // combines all of the feature columns into the Features column
                .Append(mlContext.Transforms.Concatenate("Features", "DateNumber"))

                // add the learning algorithm
                .Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "Close", featureColumnName: "Features"));

            // train the model
            var model = pipeline.Fit(dataView);

            // EVALUATE
            var crossValidate = mlContext.Regression.CrossValidate(dataView, pipeline, numberOfFolds: 5, labelColumnName: "Close");

            var metrics = new AverageMetrics
            {
                MeanAbsoluteError = crossValidate.Select(r => r.Metrics.MeanAbsoluteError).Average(),
                MeanSquaredError = crossValidate.Select(r => r.Metrics.MeanSquaredError).Average(),
                RootMeanSquaredError = crossValidate.Select(r => r.Metrics.RootMeanSquaredError).Average(),
                LossFunction = crossValidate.Select(r => r.Metrics.LossFunction).Average(),
                RSquared = crossValidate.Select(r => r.Metrics.RSquared).Average()
            };

            WriteLine($"************************************************");
            WriteLine($" Average Metrics for Regression model");
            WriteLine($"*-----------------------------------------------");
            WriteLine($" MeanAbsoluteError     {metrics.MeanAbsoluteError:0.###}");
            WriteLine($" MeanSquaredError      {metrics.MeanSquaredError:0.###}");
            WriteLine($" RootMeanSquaredError  {metrics.RootMeanSquaredError:0.###}");
            WriteLine($" LossFunction          {metrics.LossFunction:0.###}");
            WriteLine($" RSquared              {metrics.RSquared:0.###}");
            WriteLine($"************************************************\n");

            WriteLine("Wrap up");
            if (metrics.RSquared > 0.8) {
                mlContext.Model.Save(model, dataView.Schema, MODEL_FILE);
                WriteLine("The model was published.");

                Utils.SaveJsonFile(Utils.PublishPath("AverageMetrics.json"), metrics, true);

                // for ui data
                CreatePredictionsFile(mlContext, dataView, model);
            }
            else {
                WriteLine("The model is not accurate enough.");
            }
        }

        // for blazor page
        private static void CreatePredictionsFile(MLContext mlContext, IDataView dataView, ITransformer model)
        {
            var transformedData = model.Transform(dataView);
            var predictions = mlContext.Data.CreateEnumerable<StockPricePrediction>(transformedData, reuseRowObject: false);
            // save a json file all
            Utils.SaveJsonFile(Utils.PublishPath("Predictions.json"), predictions);
        }

		public static async Task PublishModelToRemoteServer()
        {
            // TODO
            // Upload AverageMetrics.json
            // Upload Predictions.json
            // Upload the model: MODEL_FILE
            // Update remote service for reload model
        }
    }
}

```

**Data exploration**

The data file, named `crudeoil_price-raw.csv` whose origin is [»»](https://www.kaggle.com/awadhi123/crude-oil-stock-price), and we use to train the model is copied in the `Data` folder of the `TrainerConsole` extension. To access the path, you can give the *Copy if new* attribute, although normally I prefer not to make a copy, and strategically access the `Data` path of the project, an issue that is resolved in the utility function `Utils.DataPath`.

Data exploration is a preliminary task whose objective is to verify integrity, data quality, strategic analysis of what is going to be done with ML. Noise is identified in the data, which can be dealt with in the pipeline, or as in this case, run a preparation method and first noise filter. In the case that concerns us, the raw data includes lines with *null* data, and the occasional anomalous value, for example, a negative price. With the `Utils.FilterNoiseLines` unit a very efficient debugging of the mentioned details is performed.

> Part of ML engineering work is to program convenient strategies to solve the problem.

**Data load**

It consists of the normal loading of a CSV file with ML.NET. As an additional and optional detail, a partial preview is shown with the `Utils.LogDataView` utility.

**Behavior pipeline and algorithms**

It is the logical core with which the ML engineer intends to solve the problem. It includes the necessary transformations and training algorithms. It is different for each problem. In ML technical terms, it is the creation of the pipeline. Let's see,

```csharp
var pipeline = mlContext.Transforms
	// the output of the model
	.CopyColumns(outputColumnName: "Label", inputColumnName: "Close")

	// transforms to numeric features
	.Append(mlContext.Transforms.Text.FeaturizeText("DateNumber", "Date"))

	// combines all of the feature columns into the Features column
	.Append(mlContext.Transforms.Concatenate("Features", "DateNumber"))

	// add the learning algorithm
	.Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "Close", featureColumnName: "Features"));
```

For this example, the only transformation that is included is the conversion of the text date to a floating number. Question that corresponds to ML.NET internal processing, in the `FeaturizeText` catalog.

On the other hand, the programmed algorithm starts from a previous analysis of applied data science, which can be simplified using a tool such as [ML.NET Model Builder](https://dotnet.microsoft.com/apps/machinelearning-ai/ ml-dotnet / model-builder).

In the case that concerns us, the FastTree or MART algorithm shows the best approximation to the objective. You can find documentation on this at [FastTreeRegressionTrainer](https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.fasttree.fasttreeregressiontrainer?view=ml-dotnet).

**Model creation**

In ML.NET the model is created with a simple line of code,

```csharp
 var model = pipeline.Fit(dataView);
```

**Validation**

There are two scenarios to validate the trained model. Here I use cross validation, which is theoretically strict and we obtain the average metrics.

**Publication**

In the development process, the publication is done by saving the model in the Data path of the server. Apart from this, the report of the metrics is published, and a complete extraction of the predictions, both in JSON format. The purpose of this is that they are to be displayed in the user interface.

To obtain the total predictions in one step, ML.NET offers us elegant functionality, a transformation and creation of the list of objects of the result with `CreateEnumerable`. This is specified in the `CreatePredictionsFile` method.

In production, the publication must include uploading the relevant files to the domain. Uploading a file to a web server from a NET console application is certainly complex. In the nuget repository I have specified how to solve it.

## Predictor

The predictor is a Blazor SPA application, which communicates with a REST Api to obtain data and display the results. Let's look at the `Server`, `Client`, and `Shared` projects from `CrudeOilStockPrice`.

**Server**

Provides an API service which is responsible for creating the instance of the prediction engine for our model. In the same API module the functions are added to deliver the data processed in the training and the report of the metrics.

*The StockPricePredictor service*

```csharp
using CrudeOilStockPrice.Shared;
using Microsoft.ML;
using System;

namespace CrudeOilStockPrice.Server.Services
{
    public class StockPricePredictor
    {
        PredictionEngine<StockPrice, StockPricePrediction> _predictionEngine;

        public StockPricePredictor()
        {
            LoadModel();
        }

        public StockPricePrediction Predict(StockPrice input)
        {
            if (_predictionEngine == null) {
                return null;
            }
            return _predictionEngine.Predict(input);
        }

        public bool LoadModel()
        {
            _predictionEngine = null;
            try {
                var mlContext = new MLContext();
                var mlModel = mlContext.Model.Load(Startup.DATA_PATH + "crudeoil-price-model.zip", out _);
                _predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(mlModel);
                return true;
            }
            catch (Exception exception) {
                Console.WriteLine("StockPricePredictor Exception:\n" + exception.Message);
            }
            return false;
        }
    }
}
```

In particular, I have exposed the `LoadModel` method in the API so that if the model is published again, there is an entry point to reload the prediction engine, since it exists as a dependency by injection

*The StockPriceController Controller*

```csharp
using CrudeOilStockPrice.Server.Services;
using CrudeOilStockPrice.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using IO = System.IO;

namespace CrudeOilStockPrice.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        StockPricePredictor _predictor;

        public StockPriceController(StockPricePredictor predictor)
        {
            _predictor = predictor;
        }

        [HttpGet("GetPredictions/{takeLast}")]
        public IEnumerable<StockPricePrediction> GetPredictions(int takeLast)
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "Predictions.json");
            var data = JsonSerializer.Deserialize<List<StockPricePrediction>>(jsonData);
            if (takeLast < 0) {
                return data;
            }
            return data.ToList().TakeLast(takeLast);
        }

        [HttpGet("GetMetrics")]
        public AverageMetrics GetMetrics()
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "AverageMetrics.json");

            return JsonSerializer.Deserialize<AverageMetrics>(jsonData);
        }

        [HttpPost("Prediction")]
        public StockPricePrediction Prediction([FromBody] StockPrice input)
        {
            return _predictor.Predict(input);
        }

        [HttpGet("ReloadModel")]
        // Run from Trainer is the model was updated.
        // In a production case it should have authorization
        public bool ReloadModel()
        {
            return _predictor.LoadModel();
        }
    }
}
```

The `GetPredictions` method, returns all the training data, plus the predictions column, in order to show the details in the front. Although the volume is remarkable, basket of 6000 records, Blazor handles it efficiently with the laudable Virtualization feature.

**Client**

It consists of a Blazor component page that has the functionality of presenting the data, displaying a results graph, the metrics report, and finally a simple form for a user to execute a prediction.

The following image shows the final result.

<img src="file:///C:/_study/Blog/Documents/Screens/bzwa_ml.png" title="" alt="" data-align="center">

![](https://github.com/harveytriana/CrudeOilStockPrice/blob/master/Screens/bzwa_ml.png)

*The Blazor PredirtorPage component*

```csharp
@page "/predictor"
@inject HttpClient _httpClient
@inject IJSRuntime _jsRunTime
@using CrudeOilStockPrice.Shared
@using System.Text.Json
@using System.Globalization
@implements IAsyncDisposable

<h1>Crude Oil Stock Price</h1>
<hr />
<p>
    An illustrative example of interface and consumption of a model created with ML.NET,
    <a href="https://www.kaggle.com/awadhi123/crude-oil-stock-price"
       target="_blank">kaggle dataset
    </a>
</p>
<br />
<div>
    @if (data == null) {
        <h5>Loading...</h5>
    }
    else {
        <h5>Crude Oil Stock Price from 01-Jan-2000 to 27-July-2020 (USD)</h5>
        <!-- DATA TABLE -->
        <table class="table table-sm" style="font-family:'Calibri'">
            <thead>
                <tr>
                    <th>Date</th>
                    <th>High</th>
                    <th>Low</th>
                    <th>Close</th>
                    <th>Prediction</th>
                </tr>
            </thead>
            <tbody>
                <Virtualize Items="data" Context="i">
                    <tr>
                        <td>@i.Date</td>
                        <td>@i.High.ToString("N2")</td>
                        <td>@i.Low.ToString("N2")</td>
                        <td>@i.Close.ToString("N2")</td>
                        <td>@i.Score.ToString("N2")</td>
                    </tr>
                </Virtualize>
            </tbody>
        </table>
        <br style="height:6px;" />
        <!-- PLOT HEADER -->
        <table style="width:100%">
            <tr>
                <td>
                    <h5>Real Data vs Machine Learning Prediction</h5>
                </td>
                <td style="text-align:right" valign="top">
                    <span>Pick Year </span>
                    <select @onchange="ChangeYear">
                        @foreach (int i in years) {
                            <option value="@i">@i</option>
                        }
                    </select>
                </td>
            </tr>
        </table>
    }
</div>

<!-- PLOT -->
<div style="height:250px;" id="container">
    <canvas id="canvas-1" hidden="@hiddenPlot"></canvas>
</div>

<!-- METRICS AND PREDICTION FORM -->
<div>
    @if (averageMetrics != null) {
        <table style="width:100%">
            <tr>
                <td valign="top">
                    <h6>Average metrics of regression</h6>
                    <!-- METRICS -->
                    <table class="table table-sm" style="font-family:'Calibri';">
                        <tr>
                            <td class="td-inf">Mean Absolute Error</td>
                            <td>@averageMetrics.MeanAbsoluteError.ToString("N4")</td>
                        </tr>
                        <tr>
                            <td class="td-inf">Mean Squared Error</td>
                            <td>@averageMetrics.MeanSquaredError.ToString("N4")</td>
                        </tr>
                        <tr>
                            <td class="td-inf">Root Mean Squared Error</td>
                            <td>@averageMetrics.RootMeanSquaredError.ToString("N4")</td>
                        </tr>
                        <tr>
                            <td class="td-inf">Loss Function</td>
                            <td>@averageMetrics.LossFunction.ToString("N4")</td>
                        </tr>
                        <tr>
                            <td class="td-inf">R-Squared</td>
                            <td>@averageMetrics.RSquared.ToString("N4")</td>
                        </tr>
                        <tr>
                            <td class="td-inf">Data Density</td>
                            <td>@data?.Count</td>
                        </tr>
                    </table>
                </td>
                <!-- separator-->
                <td style="width:20px;"></td>
                <td valign="top">
                    <h6>User Prediction</h6>
                    <!-- PREDICTION FORM -->
                    <table class="table table-sm" style="font-family:'Calibri';">
                        <tr>
                            <td class="td-inf" valign="middle">Input Date</td>
                            <td valign="middle">
                                <input type="date" @bind="date" />
                                <span class="oi oi-aperture command" @onclick="GetPrediction"></span>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <h6>
                                    Prediction Price
                                </h6>
                                <h4 style="color: rgb(33,199,90);">
                                    $ @score.ToString("N2")
                                </h4>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" style="color:slategray">@prompt</td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    }
</div>

@code {
    // ===================================
    // BlazorSpread.net
    // ===================================
    List<StockPricePrediction> data;
    List<int> years;
    AverageMetrics averageMetrics;

    IJSObjectReference module;

    bool disableCommand;
    bool hiddenPlot = true;
    string prompt;

    float score;
    DateTime date;
    int year;

    protected override async Task OnInitializedAsync()
    {
        await GetScripts();
        await GetData();
        await DrawChart();
        await GetMetrics();
        hiddenPlot = false;
        await InvokeAsync(StateHasChanged);
        await GetPrediction();
    }

    async Task GetScripts()
    {
        var jsSource = $"./jsModules/CrudeOilStockPrice.js?v={DateTime.Now.Ticks}";
        module = await _jsRunTime.InvokeAsync<IJSObjectReference>("import", jsSource);
    }

    async Task GetData()
    {
        // by UI example. Use -1 to get all
        int takeLast = -1; // all (20 years)
        var url = $"api/StockPrice/GetPredictions/{takeLast}";

        data = await _httpClient.GetFromJsonAsync<List<StockPricePrediction>>(url);
        // years 2021, 2020, ...
        years = data.Select(x => x.Year).Distinct().OrderByDescending(y => y).ToList();
        // init value
        year = years.First();
        // last date
        date = DateTime.ParseExact(data.Last().Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    async Task DrawChart(bool firstTime = true)
    {
        var labels = data.Where(x => x.Year == year).Select(x => x.Date).ToList();
        var dataSet1 = data.Where(x => x.Year == year).Select(x => x.Close).ToList();
        var dataSet2 = data.Where(x => x.Year == year).Select(x => x.Score).ToList();
        try {
            if (firstTime) {
                await module.InvokeVoidAsync("DrawChart", "canvas-1", labels, dataSet1, dataSet2);
            }
            else {
                await module.InvokeVoidAsync("UpdateChart", labels, dataSet1, dataSet2);
            }
        }
        catch (Exception e) { Console.WriteLine($"Excepton in DrawChart: {e.Message}"); }
    }

    async Task GetMetrics()
    {
        var url = "api/StockPrice/GetMetrics";
        averageMetrics = await _httpClient.GetFromJsonAsync<AverageMetrics>(url);
    }

    async Task GetPrediction()
    {
        if (disableCommand) {// prevent multiple click
            return;
        }
        disableCommand = true;
        prompt = "Processing...";
        try {
            var url = "api/StockPrice/Prediction";
            var response = await _httpClient.PostAsJsonAsync<StockPrice>(url, new StockPrice
            {
                Date = date.ToString("yyyy-MM-dd")
            });
            var json = await response.Content.ReadAsStringAsync();
            var so = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var predictionData = JsonSerializer.Deserialize<StockPricePrediction>(json, so);
            score = predictionData != null ? predictionData.Score : 0;
        }
        catch {
            prompt = "Exception in GetPrediction(...)";
        }
        await Task.Delay(1000);
        disableCommand = false;
        prompt = "Press update-icon to calculate.";
    }

    async Task ChangeYear(ChangeEventArgs e)
    {
        var y = int.Parse(e.Value.ToString());
        if (year != y) {
            year = y;
            await DrawChart(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await module.DisposeAsync();
    }
}

```

**DETAILS**

The details that I show on this page are several and important. You will find many TIPS about Blazor in one place.

- JavaScript interoperability to use [ChartJs](https://www.chartjs.org/) in isolated mode. Data sets are sent from C # to JavaScript functions. The `CrudeOilStockPrice.js` module was designed to accept updating the chart with an animation, which gives a pleasant user experience. The graph was programmed to show the curves with a filter of per year: which is displayed from an HTML selection list and is managed in the pertinent event; here applies the remarkable optimization that was introduced by adding the `Year` property.

> From my point of view, Blazor does not intend to compete with JS when there are libraries as sophisticated as ChartJS, on the contrary, we benefit enormously from that. Personally, I do not agree to mount a C # layer to use libraries like these, it is an unfair and unnecessary burden, moreover it is usually limited.

- Virtualization to effectively load the data and show the origin of the model. The volume is considerable, there are 20 years of daily reports with about 6000 records. However, for Blazor that is not a problem. Initially I programmed a filter to not take the entire volume, but a discrete number of records, but with virtualization this is more than enough.

- Reveals the way a user prediction is obtained. By virtue of shared classes, structures are available without writing more code. A call is made to the available API that encapsulates the server-side ML.NET management.

- Shows the report of the model's training metrics. This is done through reading a server-side JSON file.

- A simple form is arranged in an HTML table, where the user enters a date, and through a command a request is sent to the server so that it returns the prediction.

> PS Web design experts will forgive my little CSS knowledge. I always find how to solve it by laying out HTML tables. In a production case, the design would be passed on to an expert on the matter.

### Conclusions

The ML engineering treatment applied to the problem gave excellent results with a pressure of ~ 98%; Commendable considering the volume of data, and that is a simplistic analysis. The *FastTree* algorithm is ideal in this case, and it is difficult for someone else to beat it. As you can see from the actual data versus prediction curves, the curve that the ML generates is accurate. The treatment given in this example is geared more towards the software writing demonstration than the actual problem. As we know, the price of crude would be subject to more complex variables, such as globalization, geopolitics and others.

Blazor is one of the best framework options. The spectacular reactivity and communication with services and with JavaScript make Blazor an excellent paradigm for this type of solution.

---

This article belongs to the Blog »» [BlazorSpread.net](https://www.blazorspread.net) 

---

*References*

- [Documentación ML.NET](https://docs.microsoft.com/es-es/dotnet/machine-learning/)

- [Crude Oil Stock Price](https://www.kaggle.com/awadhi123/crude-oil-stock-price)

- [FastTreeRegressionTrainer]( https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.fasttree.fasttreeregressiontrainer?view=ml-dotnet).

`MIT license. Author: Harvey Triana. Contact: admin @ blazorspread.net`

---

*Last edition: 05-30-2021*

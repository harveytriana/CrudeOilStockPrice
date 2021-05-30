// ==================================
// BlazorSpread.net
// ===================================
import '../lib/chart.min.js'

var _ctx, _chart, _config;

export function DrawChart(canvasId, labels, dataSet1, dataSet2) {
    _ctx = document.getElementById(canvasId);
    _config = getConfig(labels, dataSet1, dataSet2)
    _chart = new Chart(_ctx, _config);
}

function getConfig(labels, dataSet1, dataSet2) {
    return {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {// observed data
                    data: dataSet1,
                    pointRadius: 3,
                    borderColor: 'rgba(64,116,120,0.8)',
                    backgroundColor: 'transparent',
                    fill: false,
                    showLine: false
                },
                {// prediction curve
                    data: dataSet2,
                    pointRadius: 0,
                    fill: false,
                    borderColor: 'rgb(33,199,90)',
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            legend: {
                display: false
            },
            animation: {
                duration: 500,
                easing: 'linear'
            },
            scales: {
                yAxes: [{
                    ticks: {
                        fontSize: 10,
                        min: 10,
                        // max: 120
                    }
                }],
                xAxes: [{
                    ticks: {
                        fontSize: 10,
                        fontFamily: 'Lucida Console',
                        fontColor: 'transparent'
                    }
                }]
            },
            tooltips: {
                mode: 'label'
            }
        }
    }
}

export function UpdateChart(labels, dataSet1, dataSet2) {
    _config.data.labels = labels;
    _config.data.datasets[0].data = dataSet1;
    _config.data.datasets[1].data = dataSet2;
    _chart.update();
}

// FIT PLOT WIDTH
const chartContainer = document.getElementById('container');

function fitPlotWidth() {
    let ss = 270;
    if (window.innerWidth < 640) ss = 50; // side menu is hide
    chartContainer.setAttribute('style', 'width:' + (window.innerWidth - ss) + 'px');
}

window.onresize = fitPlotWidth;
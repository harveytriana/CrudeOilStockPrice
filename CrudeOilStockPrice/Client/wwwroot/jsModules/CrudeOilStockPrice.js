import '../lib/chart.min.js'

export function DrawChart(canvasId, labels, dataSet1, dataSet2) {
    let ctx = document.getElementById(canvasId);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    data: dataSet1,
                    pointRadius: letPointRadius(),
                    borderColor: 'rgba(64,128,128,0.7)',
                    backgroundColor: 'transparent',
                    fill: false,
                    showLine: false
                },
                {
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
                        min: 0,
                        max: 100
                    },
                    gridLines: {
                        drawBorder: false
                    }
                }],
                xAxes: [{
                    ticks: {
                        fontSize: 10,
                        fontFamily: 'Lucida Console',
                        fontColor: 'transparent'
                    },
                    gridLines: {
                        drawBorder: false
                    }
                }]
            },
            tooltips: {
                mode: 'label'
            }
        }
    });

    function letPointRadius() {
        console.log(labels.length);
        if (labels.length <= 100) {
            return 3;
        }
        return 4;
    }
}
// FIT PLOT WIDTH
const chartContainer = document.getElementById('container');

function fitPlotWidth() {
    let ss = 270;
    if (window.innerWidth < 640) ss = 50; // side menu is hide
    chartContainer.setAttribute('style', 'width:' + (window.innerWidth - ss) + 'px');
}

window.onresize = fitPlotWidth;
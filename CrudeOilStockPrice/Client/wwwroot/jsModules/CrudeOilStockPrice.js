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
                    pointRadius: 4,
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
            responsive: false,
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
                        beginAtZero: true
                    }
                }],
                xAxes: [{
                    ticks: {
                        fontSize: 10,
                        // display: false,
                        fontFamily: 'Lucida Console',
                        fontColor: 'transparent'
                    },
                    //gridLines: {
                    //    display: false
                    //}
                }]
            },
            tooltips: {
                mode: 'label'
            }
        }
    });
}


function getRandomColor(opacy) {
    var color = 'rgba(';
    for (var i = 0; i < 3; i++) {
        color += Math.floor(Math.random() * 255) + ',';
    }
    color += opacy.toString() + ')'; // add the transparency
    return color;
}


function randomHSL() {
    return "hsla(" + ~~(360 * Math.random()) + "," + "70%," + "70%,1)"
}

function getRandomColor1() {
    return '#' + Math.floor(Math.random() * 16777215).toString(16);
}

import '../lib/chart.min.js'

export function DrawChart(canvasId, labels, values) {
    //let colors = [];
    let n = labels.length;

    let ctx = document.getElementById(canvasId);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    borderColor: ['rgb(163,141,28)'],
                    data: values,
                    pointRadius: 0,
                    fill: false
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
                xAxes: [{
                    ticks: {
                        display: false
                    }
                }],
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

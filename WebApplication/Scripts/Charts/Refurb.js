function getBaseColors() {
    // rgb colors pallete
    var baseColors = [
        [234, 53, 86], /* http://www.colourlovers.com/palette/2310234/Bright_Chart */
        [97, 210, 214],
        [255, 228, 77],
        [181, 225, 86],
        [130, 24, 124],

        [255, 164, 1], /* http://www.colourlovers.com/palette/4287597/Angie */
        [45, 78, 96],

        [155, 89, 39], /* http://www.colourlovers.com/palette/2941063/Dux */
        [19, 154, 34],
        [228, 141, 45],

        [53, 166, 234], /* http://www.colourlovers.com/palette/2816315/Floral_Hype */
        [155, 232, 77],
        [191, 105, 244],

        [161, 7, 14], /* http://www.colourlovers.com/palette/2091175/Apple_Red */
        [105, 10, 5]
    ];

    return baseColors;
};


Chart.plugins.register({
    afterDatasetsDraw: function (chartInstance, easing) {
        var options = chartInstance.config.options;
        if (!options.labelled) { return; }
        var padding = 5;
        var ctx = chartInstance.chart.ctx;
        _.each(chartInstance.data.datasets, function (dataset, i) {
            var fontSize = 9; 
            var fontStyle = 'bold';
            var fontFamily = 'sans-serif';

            ctx.font = Chart.helpers.fontString(fontSize, fontStyle, fontFamily);
            ctx.fillStyle = dataset.borderColor;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'bottom';

            var meta = chartInstance.getDatasetMeta(i);

            if (!meta.hidden) {
                _.each(meta.data, function (element, index) {
                    var dataString = dataset.data[index].toString();
                    if (dataString != 0) {
                        var position = element.tooltipPosition();
                        ctx.fillText(dataString, position.x, position.y - padding);
                    }
                });
            }
        });
    }
});
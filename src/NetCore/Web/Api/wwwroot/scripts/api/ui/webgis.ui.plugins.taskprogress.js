webgis.ui.definePlugin("webgis_taskProgress", {
    defaults: {
        map: null,
        taskId: null,
        message: null,
        callback: null,
        autoCallack: null
    },
    init: function () {
        let $taskProgressList = this.$el.find(".webgis-task-progress-list");
        if ($taskProgressList.length === 0) {
            $("<ul>").addClass("webgis-task-progress-list").appendTo(this.$el);
        }
    },
    destroy: function () {
        this.$el.off('.webgis_taskprogress');
    },
    methods: {
        addTask: function (options) {
            //console.log('addTask', options); 
            const $ = this.$;
            const o = options || this.options;
            o.taskId = o.taskId || webgis.guid();
            o.message = o.message || webgis.l10n.get("processing");

            let $taskProgressList = this.$el.find(".webgis-task-progress-list");

            $taskProgress = $("<li>")
                .addClass("webgis-task-progress-item")
                .attr("data-task-id", o.taskId)
                .text(o.message)
                .appendTo($taskProgressList);
        },
        finsihTask: function (options) {
            //console.log('finsihTask', options);
            const $ = this.$;
            const o = options || this.options;
            const $taskProgress = this.$el.find(".webgis-task-progress-item[data-task-id='" + o.taskId + "']");

            $taskProgress.addClass("finished"); 

            if (options.callback) {
                if (options.callbackArgs && options.callbackArgs.success === false) {
                    $taskProgress.addClass("error");
                }
                $taskProgress
                    .data('options', options)
                    .on('click.webgis_taskprogress', function (e) {
                        e.stopPropagation();
                        const options = $(this).data('options');
                        $(this).remove();

                        options.callback(options.callbackArgs);
                    });

                if (options.fireCallback) {
                    $taskProgress.trigger('click');
                }
            }
        }
    }
});

webgis.ui.addTaskProgress = function (options) {
    var $mapContainer = $(".webgis-container").find("#map");
    var $taskProgressContainer = $mapContainer.find(".webgis-task-progress-container");

    if ($taskProgressContainer.length === 0) {
        $taskProgressContainer = $("<div>")
            .addClass("webgis-task-progress-container")
            .appendTo($mapContainer)
            .webgis_taskProgress(options);
    }

    $taskProgressContainer.webgis_taskProgress('addTask', options);
};

webgis.ui.finishTaskProgress = function (options) {
    var $mapContainer = $(".webgis-container").find("#map");
    var $taskProgressContainer = $mapContainer.find(".webgis-task-progress-container");
    if ($taskProgressContainer.length === 0) return;

    $taskProgressContainer.webgis_taskProgress("finsihTask", options);
};
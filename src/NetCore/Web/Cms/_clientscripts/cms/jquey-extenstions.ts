module UI {

    //
    //  Navbar
    //

    export class NavbarItem {
        name: string;
        path: string;
    }
    interface INavbarOptions {
        items: NavbarItem[];
    }

    export class NavbarOptions implements INavbarOptions {
        items: NavbarItem[];

        constructor(items: NavbarItem[]) {
            this.items = items;
        }
    }

    export class Navbar {
        element: JQuery;
        options: INavbarOptions;

        constructor(element: JQuery, options: INavbarOptions) {
            this.element = element;
            this.options = options;

            this.OnCreate();
        }

        OnCreate() {
            $(this.element).empty().addClass('cms-navbar');

            for (var i = 0; i < this.options.items.length; i++) {
                var item = this.options.items[i];

                var $item = $("<div>")
                    .addClass('item')
                    .attr('data-path', item.path)
                    .appendTo(this.element);

                $("<div></div>")
                    .text(item.name)
                    .addClass('text')
                    .appendTo($item);
                if (i > 0 && i == this.options.items.length - 1) {
                    $('<div></div>')
                        .addClass('pin')
                        .data('pin-name', this.options.items[i - 1].name + '/' + item.name)
                        .data('pin-path', item.path)
                        .appendTo($item)
                        .click(function (e) {
                            //e.stopPropagation();  // dont stop... refresh after click...
                            CMS.addPin($(this).data('pin-name'), $(this).data('pin-path'));
                        });
                }
                $('<div></div>').addClass('right').appendTo($item);

                this.CheckSize();
            }
        }

        CheckSize() {
            var width = this.element.width();

            var itemsWidth = 0;
            this.element.children('.item').each(function (i, e) {
                var $item = $(e);
                if (!$item.hasClass('hidden')) {
                    itemsWidth += $item.width();
                }
            });

            if (itemsWidth > width) {
                this.element.children(':not(".hidden")').first().addClass('hidden');
                this.CheckSize();
            }
        }
    }

    // 
    // PinList
    //
    export class PinListItem {
        name: string;
        path: string;
    }

    export class PinListOptions {
        items: PinListItem[];

        constructor(items: PinListItem[]) {
            this.items = items;
        }
    }

    export class PinList {
        element: JQuery;
        options: PinListOptions;

        constructor(element: JQuery, options: PinListOptions) {
            this.element = element;
            this.options = options;

            var $this = this.element;
            if (!$this.hasClass('cms-navtree')) {
                $this.empty().addClass('cms-navtree');
            }
            var $list = $this.children('.pin-list-items');
            if ($list.length === 0) {
                $list = $("<div>").addClass('pin-list-items').appendTo($this);
            }

            if (options.items) {
                this.OnCreate(options.items);
            } else {
                var me = this;
                CMS.allPins(function (result) {
                    me.OnCreate((result || []).reverse());
                });
            }
        }
        OnCreate(items: any) {
            var $this = this.element;
            var $list = $this.children('.pin-list-items');
            
            for (var i = 0; i < items.length; i++) {
                var item = items[i];

                if ($list.children("[data-path='" + item.path + "']").length === 0) {
                    this.CreateListItem($list, item);
                }
            }
        }
        CreateListItem($parent: JQuery, item: PinListItem) {
            $parent.removeClass('collapsed');

            var $item = $("<div><div class='text'><span class='pin'>&nbsp;&nbsp;&nbsp;</span>&nbsp;" + CMS.encodeHtmlString(item.name) + "</div><div>")
                .addClass('cms-pin-item item')
                .attr('data-path', item.path)
                .appendTo($parent);

            $item
                .click(function () {
                    CMS.updateContent($(this).attr('data-path'));
                });

            $item.find('.pin').click(function (e) {
                e.stopPropagation();

                CMS.removePin($(this).closest('.cms-pin-item').attr('data-path'));
                $(this).closest('.cms-pin-item').remove();
            });
        }
    }


    //
    // NavTree
    //

    export class NavtreeItem {
        name: string;
        path: string;
        obsolete?: boolean;
    }
    interface INavtreeOptions {
        items: NavtreeItem[];
        nodes: ContentNode[];
        path: string;
    }

    export class NavtreeOptions implements INavbarOptions {
        items: NavtreeItem[];
        nodes: ContentNode[];

        constructor(items: NavbarItem[], nodes: ContentNode[]) {
            this.items = items;
            this.nodes = nodes;
        }
    }

    export class Navtree {
        element: JQuery;
        options: INavtreeOptions;

        constructor(element: JQuery, options: INavtreeOptions) {
            this.element = element;
            this.options = options;

            this.OnCreate();
        }

        OnCreate() {
            var $this = this.element;
            if (!$this.hasClass('cms-navtree')) {
                $this.empty().addClass('cms-navtree');
            }
            if ($this.children('.cms-treenode-nodes').length === 0) {
                $("<div>").addClass('cms-treenode-nodes').appendTo($this);
            }

            for (var i = 0; i < this.options.items.length; i++) {
                var item = this.options.items[i];

                var $node = this.FindTreeNode($this, item.path);
                if ($node.length > 0)
                    continue;

                this.CreateTreeNode($this, item);
            }

            for (var i = 0; i < this.options.nodes.length; i++) {
                var node = this.options.nodes[i];
                if (node.hasChildren === false)
                    continue;

                var $node = this.FindTreeNode($this, node.path);
                if ($node.length == 0) {
                    var navtreeItem = new NavtreeItem();
                    navtreeItem.name = node.aliasname;
                    navtreeItem.path = node.path;
                    navtreeItem.obsolete = node.obsolete;
                    this.CreateTreeNode($this, navtreeItem);
                }
            }

            $this.find('.cms-treenode.selected').removeClass('selected');
            if (!this.options.path)
                this.options.path = '/';
            if (this.options.path.indexOf('/') !== 0)
                this.options.path = '/' + this.options.path;
            this.FindTreeNode($this, this.options.path).addClass('selected');
        }

        FindTreeNode($rootNode: JQuery, path: string): JQuery {
            if (!path)
                path = '/';

            var $node = $rootNode.find(".cms-treenode[data-path='" + path + "']");
            return $node;
        }

        CreateTreeNode($rootNode: JQuery, item: NavtreeItem) {
            var pathElements = item.path.split('/');
            var currentPath = '';

            for (var i = 0, to = pathElements.length; i < to; i++) {
                currentPath += (currentPath.length > 1 ? '/' : '') + pathElements[i];
                if (currentPath === '' || currentPath.indexOf('/') !== 0)
                    currentPath = '/' + currentPath;

                var $parent = this.FindTreeNode($rootNode, currentPath);
                if ($parent.length > 0) {
                    $rootNode = $parent;
                }
                else {
                    $rootNode.removeClass('collapsed');
                    $rootNode = $("<div><div class='text'><span class='plus'>&nbsp;&nbsp;&nbsp;</span>&nbsp;" + CMS.encodeHtmlString(item.name) + "</div><div>")
                        .addClass('cms-treenode collapsed')
                        .attr('data-path', currentPath)
                        .appendTo($rootNode.children('.cms-treenode-nodes'));
                    if (item.obsolete === true) {
                        $rootNode.addClass('obsolete')
                    }
                    $("<div>").addClass('cms-treenode-nodes').appendTo($rootNode);

                    $rootNode.children('.text').children('.plus')
                        .click(function (e) {
                            var $node = $(this).closest('.cms-treenode');
                            if (!$node.hasClass('collapsed')) {
                                e.stopPropagation();
                                $node.addClass('collapsed');
                            } else {
                                var $childNodes = $node.find('.cms-treenode-nodes').children('.cms-treenode');
                                if ($childNodes.length > 0)  // node nur öffnen
                                    e.stopPropagation();
                                $node.removeClass('collapsed');
                            }
                        });
                }
            }
        }
    }

    //
    //  Toolbar
    //

    export class ToolbarItem {
        action: string;
        name: string;
        prompt: string;
        path: string;
    }

    export class ToolbarOptions {
        tools: ToolbarItem[];
        orderable: boolean;
    }

    export class Toolbar {
        element: JQuery;
        options: ToolbarOptions;

        constructor(element: JQuery, options: ToolbarOptions) {
            this.element = element;
            this.options = options;

            this.OnCreate();
        }

        OnCreate() {
            $(this.element).empty().addClass('cms-toolbar');
            if (!this.options.tools)
                return;

            if (this.options.orderable)
            {
                let $orderTool = $("<div>")
                    .addClass('tool order asc')
                    .appendTo(this.element);
                $("<div>")
                    .text('AZ')
                    .addClass('text')
                    .appendTo($orderTool);

            }
            for (let i = 0; i < this.options.tools.length; i++) {
                let tool = this.options.tools[i];

                let $tool = $("<div>")
                    .addClass('tool ' + tool.action)
                    .attr('data-path', tool.path)
                    .attr('data-name', tool.name)
                    .attr('data-action', tool.action)
                    .appendTo(this.element);
                $("<div>")
                    .text(tool.prompt)
                    .addClass('text')
                    .appendTo($tool);
            }
        }
    }

    //
    //  ContentNodes
    //

    export class ContentNode {
        name: string;
        aliasname: string;
        path: string;
        hasChildren: boolean;
        target: string;
        isTargetValid: boolean;
        hasProperties: boolean;
        isDeletable: boolean;
        isRefreshable: boolean;
        isCopyable: boolean;
        obsolete: boolean;
        isRecommended?: boolean;
        isRequired?: boolean;
        hasContent: boolean;
        hasSecurity: boolean;
        hasSecurityRestrictions: boolean;
        hasSecurityExclusiveRestrictions: boolean;
        primaryproperty_value: string;
    }
    export class ContentNodeOptions {
        nodes: ContentNode[];
        orderable: boolean;
        path: string;
    }

    export class ContentNodes {
        element: JQuery;
        options: ContentNodeOptions;

        constructor(element: JQuery, options: ContentNodeOptions) {
            this.element = element;
            this.options = options;

            this.OnCreate();
        }

        OnCreate() {
            $(this.element).empty().addClass('cms-content');
            if (this.options.orderable)
                $(this.element).addClass('orderable');

            var parentPath = null;
            if (this.options.path) {
                if (this.options.path.lastIndexOf('/') > 0) {
                    parentPath = this.options.path.substr(0, this.options.path.lastIndexOf('/'));
                } else {
                    parentPath = '';
                }
            }

            var $ul = $("<ul>").appendTo(this.element);

            if (parentPath !== null) {
                var $upNode = $("<li>")
                    .addClass('node')
                    .attr('data-name', '..')
                    .attr('data-path', parentPath)
                    .attr('data-haschildren','true')
                    .addClass('parent up')
                    .appendTo($ul);
                $("<div>[..]</div>")
                    .addClass('title')
                    .appendTo($upNode);
            }

            for (var i = 0; i < this.options.nodes.length; i++) {
                var node = this.options.nodes[i];

                var $node = $("<li>")
                    .addClass('node')
                    .attr('data-name', node.name)
                    .attr('data-path', node.path)
                    .attr('data-haschildren', node.hasChildren.toString())
                    .attr('data-searchitem', node.aliasname.toLocaleLowerCase() + ' ' + node.path)
                    .css({ 'top': -i * 40, 'left': /*i*40*/0, 'opacity': 0 })
                    .appendTo($ul);

                if (node.hasChildren === true) {
                    $node.addClass('parent');
                }
                if (node.obsolete === true) {
                    $node.addClass('obsolete');
                }
                if (node.target != null) {
                    $node.addClass('link');
                    $node.attr('data-target', node.target);

                    if (node.isTargetValid === false) {
                        $node.addClass('link-invalid');
                    }
                }
                if (node.hasContent === true) {
                    $node.addClass('has-content');
                }
                if (node.isRecommended === true) {
                    $node.addClass('is-recommended');
                }
                if (node.isRequired === true) {
                    $node.addClass('is-required');
                }

                if (node.name === ".")
                    $node.addClass('current');

                var $nodeTools = $('<div>').addClass('node-tools').appendTo($node);
                $("<div>").addClass('node-menu').appendTo($nodeTools)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).parent().toggleClass('expanded');
                    });

                if (node.isCopyable === true) {
                    $("<div>").addClass('node-copy').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                    $("<div>").addClass('node-cut').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                }
                if (node.isRefreshable === true) {
                    $("<div>").addClass('node-refresh').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                }
                if (node.isDeletable === true) {
                    $("<div>").addClass('node-delete').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                }
                if (node.target != null) {
                    $("<div>").addClass('node-target-properties').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                };
                if ((node.hasChildren === true/* || node.target != null*/) && node.hasProperties === true) {
                    $("<div>").addClass('node-properties').appendTo($nodeTools)
                        .click(function () {
                            $(this).parent().removeClass('expanded');
                        });
                }

                $("<div>")
                    .addClass('node-security')
                    .addClass(node.hasSecurity === true ? 'node-has-security' : '')
                    .addClass(node.hasSecurityRestrictions === true ? 'node-has-security-restrictions' : '')
                    .addClass(node.hasSecurityExclusiveRestrictions === true ? 'node-has-security-exclusive-restrictions' : '')
                    .appendTo($nodeTools)
                    .click(function () {
                        $(this).parent().removeClass('expanded');
                    });

                $("<div></div>")
                    .text(node.aliasname)
                    .addClass('title')
                    .appendTo($node);

                if (node.primaryproperty_value) {
                    $("<div>")
                        .addClass('primary-property-value')
                        .text(node.primaryproperty_value)
                        .appendTo($node);
                }

                //console.log(node);
                if (node.target != null) {
                    $("<div>")
                        .addClass('target')
                        .text('=> ' + node.target)
                        .appendTo($node);
                } else if(node.path) {
                    $("<div>")
                        .addClass('target')
                        .text(node.path)
                        .appendTo($node);
                }
            }

            if (this.options.orderable === true) {
                CMS.makeSortable($ul);
            }

            // animate (trigger transision)
            setTimeout(function () {
                $ul.children('li').css({ 'top': 0, 'left': 0, 'opacity': 'inherit' });
            }, 0);
            
        }
    }

    // 
    //  ModalDialog
    //

    export class ModalDialogOptions {
        title: string;
        onLoad: (content: JQuery, modal: JQuery) => any;
        onClose?: (modal:JQuery) => any;
        onCommit?: (sender: JQuery, modal: JQuery) => any;
    }

    export class ModalDialog {
        element: JQuery;
        options: ModalDialogOptions;

        constructor(element: JQuery, options: ModalDialogOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                this[method].apply(this);
            } else {
                this.OnCreate();
            }
        }

        OnCreate() {
            var countModals = $('.modaldialog-modal').length;

            var $blocker = $('<div>').addClass('modaldialog-blocker').appendTo(this.element);
            if (CMS.inIFrame() === true) {
                $blocker.addClass('iframed');
            }
            var $modal = $("<div>").addClass('modaldialog-modal')
                .data('options', this.options)
                .css('margin-top', 38 * countModals)
                .css('height', 'calc(100% - ' + 38 * countModals + 'px')
                .css({ 'top': -100, 'opacity': 0 })
                .appendTo($blocker);

            var title = CMS.encodeHtmlString(this.options.title);
            for (let i = 0; i < countModals; i++) {
                title = "»&nbsp;" + title;
            }

            var $title = $("<div>" + title + "<div>")
                .addClass('modaldialog-title')
                .appendTo($modal);
            var $body = $("<div>").addClass('modaldialog-body').appendTo($modal);
            if (this.options.onCommit) {
                $modal.addClass('has-buttons');
                var $buttons = $("<div>").addClass('modaldialog-buttons').appendTo($modal);
                var $commit = $("<button>Übernehmen</button>")
                    .addClass('modaldialog-button-commit')
                    .appendTo($buttons)
                    .click(function () {
                        CMS.commitModal(this);
                    });
            }

            var $close = $("<div>✖</div>").addClass('modaldialog-close').appendTo($title);
            $close.click(function () {
                CMS.closeModal(this);
            });

            if (this.options.onLoad)
                this.options.onLoad($body, $modal);

            // animate transition
            setTimeout(function () {
                $modal.css({
                    'top': 0,
                    'opacity': 1
                });
            },0);
        }

        Close() {
            let modal = this.GetModal(this.element);
            let options = <ModalDialogOptions>modal.data('options');
 
            if (options && options.onClose)
                options.onClose(modal);

            modal.closest('.modaldialog-blocker').remove();
        }

        Commit() {
            let modal = this.GetModal(this.element);
            let options = <ModalDialogOptions>modal.data('options');

            if (options && options.onCommit)
                options.onCommit(this.element, modal);
        }

        GetModal(element: JQuery) {
            if (element.hasClass('.modaldialog-modal'))
                return element;

            return element.closest('.modaldialog-modal');
        }
    }

    // NavTree

    export class LazyNavTreeOptions {
        path: string;
        name: string;
        singleSelect: boolean;
    }

    class TreeNode {
        name: string;
        path: string;
        aliasname: string;
        selectable: boolean;
        nodes: TreeNode[];
    };

    export class LazyNavTree {
        element: JQuery;
        options: LazyNavTreeOptions;

        constructor(element: JQuery, options: LazyNavTreeOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                this[method].apply(this);
            } else {
                this.OnCreate();
            }
        }

        OnCreate() {
            this.element.addClass("cms-lazy-navtree");

            $("<div>").addClass('button-back').appendTo(this.element)
                .click(function (e) {
                    new LazyNavTree($(this).closest('.cms-lazy-navtree'), null, 'Back');
                });
            $("<input readonly='readlony' name='path' type='text' />").appendTo(this.element);
            $("<div>").addClass('loading').appendTo(this.element);
            $("<ul>").addClass('navtree-list')
                .data('singleSelect', this.options.singleSelect)
                .data('data',[])
                .appendTo(this.element);
            var navTree = this;



            CMS.api('linknodes', { path: this.options.path, name: this.options.name },
                function (result) {
                    navTree.element
                        .data('path', '')
                        .data('rootnodes', result.treenodes);

                    navTree.Refresh();
                }, null
            );
        }

        Refresh() {
            let treeNodes = <TreeNode[]>this.element.data('rootnodes');
            let pathNames = <string[]>this.element.data('path').toString().split('/');

            for (var p in pathNames) {
                var pathName = pathNames[p];

                if (!pathName || pathName === '')
                    break;

                for (var n in treeNodes) {
                    if (treeNodes[n].name == pathName) {
                        treeNodes = treeNodes[n].nodes;
                        break;
                    }
                }
            }

            this.element.find("input[name='path']").val(this.element.data('path'));
            this.element.children('.loading').remove();
            var $list = this.element.find('ul.navtree-list').empty();

            for (var n in treeNodes) {
                var treeNode = treeNodes[n];

                var $li = $("<li></li>")
                    .text(treeNode.aliasname)
                    .attr('data-path', treeNode.path)
                    .attr('data-name', treeNode.name)
                    .addClass(treeNode.nodes != null ? 'parent' : '')
                    .addClass(treeNode.selectable === true ? 'selectable' : '')
                    .appendTo($list)
                    .click(function (e) {
                        var $this = $(this);
                        var $list = $this.closest('ul.navtree-list');
                        if ($this.hasClass('selectable')) {
                            if ($list.data('singleSelect') === true) {
                                var selected = $this.hasClass('selected');
                                $list.children('.selected').removeClass('selected');
                                $list.data('data', []);
                                if (!selected)
                                    $this.addClass('selected');
                            } else {
                                $this.toggleClass('selected');
                            }

                            $list.data('data')[$this.attr('data-path')] = $this.hasClass('selected');
                        }
                        if ($this.hasClass('parent')) {
                            var $element = $this.closest('.cms-lazy-navtree');
                            $element.data('path', $this.attr('data-path'));
                            new LazyNavTree($element, null, "Refresh");
                        }
                    });
            }

            if ($list.children('li').length == 1 && $list.children('li').hasClass('parent') && !$list.children('li').hasClass('selectable'))
                $list.children('li').trigger('click');
        }

        Back() {
            var path = <string>this.element.data('path');
            var newPath = <string>null;
            
            while (true) {
                var index = path.lastIndexOf('/');
                if (index > 0)
                    path = path.substr(0, index);

                var pathNames = path.split('/');

                var nodes = <TreeNode[]>this.element.data('rootnodes');
                for (var p in pathNames) {
                    var pathName = pathNames[p];

                    for (var n in nodes) {
                        var node = nodes[n];
                        if (node.name == pathName) {
                            nodes = node.nodes;
                            break;
                        }
                    }
                }

                if (nodes && nodes.length > 1) {
                    newPath = path;
                    break;
                }

                if (index <= 0)
                    break;
            }

            if (newPath != null) {
                this.element.data('path', newPath);
                this.Refresh();
            }
        }

        RefreshData() {
            var parent = this.element;
            var data = [];

            var $list = this.element.find('ul.navtree-list')
            var listData = $list.data('data');

            for (var l in listData) {
                if (listData[l]=== true)
                data.push(l);
            }

            return parent.data('navtree-data', data);
        }
    }

    // Hourglass

    export class HourglassOptions {
        title:string
    }

    export class Hourglass {
        element: JQuery;
        options: HourglassOptions;

        constructor(element: JQuery, options: HourglassOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                this[method].apply(this);
            } else {
                this.OnCreate();
            }
        }

        OnCreate() {
            var $blocker = $("<div>").addClass('cms-hourglass-blocker').appendTo(this.element);
            var $loader = $("<div>").addClass('loader').appendTo($blocker);

            $loader.html(this.options.title);
        }

        Close() {
            this.element.find('.cms-hourglass-blocker').remove();
        }
    }

    // Description

    export class DescriptionPanelOptions {
        id: string;
        text: string;
    }

    export class DescriptionPanel {
        element: JQuery;
        options: DescriptionPanelOptions;

        constructor(element: JQuery, options: DescriptionPanelOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                this[method].apply(this);
            } else {
                this.OnCreate();
            }
        }

        OnCreate() {
            var $panel = this.element.find(".cms-description-panel[data-id='" + this.options.id + "']");
            if ($panel.length === 0) {
                $panel = $("<div>")
                    .addClass('cms-description-panel')
                    .attr('data-id', this.options.id)
                    .appendTo(this.element);
                var $header = $("<div>")
                    .addClass('cms-description-header')
                    .appendTo($panel);
                $("<div>")
                    .addClass('cms-description-switcher')
                    .appendTo($header)
                    .click(function () {
                        var $panel = $(this).closest('.cms-description-panel').toggleClass('collapsed');
                        
                        if (window.localStorage) {
                            window.localStorage.setItem('cms-desc-' + $panel.attr('data-id'), $panel.hasClass('collapsed') ? '0' : '1');
                        }
                    });
                $("<div>")
                    .addClass('cms-description-content')
                    .appendTo($panel);
            }

            if (window.localStorage) {
                if (window.localStorage.getItem('cms-desc-' + this.options.id) === '0')
                    $panel.addClass('collapsed');
            }

            $panel.children('.cms-description-content').html(this.options.text);
        }

        RemoveAll() {
            this.element.find('.cms-description-panel').remove();
        }
    }

    // Secret Selector
    export class SecretSelectorPanelOptions {

    }

    export class SecretSelectorPanel {
        element: JQuery;
        options: SecretSelectorPanelOptions;

        constructor(element: JQuery, options: SecretSelectorPanelOptions, method?: string) {
            this.element = element;
            this.options = options;

            if (method) {
                this[method].apply(this);
            } else {
                this.OnCreate();
            }
        }

        OnCreate() {
            var $container = $(this.element);

            var $toolbar = $container.children('.cms-toolbar');
            if ($toolbar.length === 0) {
                $toolbar = $("<div>")
                    .addClass('cms-toolbar')
                    .insertBefore($container.children().first());
            }

            var $panel = $container.children('.secret-selector-list-conatiner');
            if ($panel.length === 0) {
                $panel = $("<div>")
                    .addClass('secret-selector-list-conatiner')
                    .insertAfter($toolbar);
            }

            $panel.empty();

            $("<div>")
                .text("...")
                .addClass("tool load-secrets")
                .appendTo($toolbar)
                .click(function () {
                    var $list = $(this).parent().parent().find('.cms-secretselector-list');

                    if ($list.children().length > 0) {
                        $(this).text("...");
                        $list.empty();
                    } else {
                        $(this).text("X");
                        CMS.loadSecretPlaceholders(function (result) {
                            $.each(result, function (i, secretPlaceholder) {
                                var $item = $("<li>")
                                    .addClass("cms-secretselector-list-item")
                                    .appendTo($list)
                                    .mouseout(function () {
                                        $(this).find('.tooltiptext').text('Copy placeholder');
                                    })
                                    .click(function (e) {
                                        e.stopPropagation();

                                        var $text = $(this).children('.text');

                                        //(<any>this).select();
                                        //(<any>this).setSelectionRange(0, 99999);
                                        (<any>navigator).clipboard.writeText($text.text());

                                        $(this).find('.tooltiptext').text("Copied: " + $text.text().substr(0, 20) + '...');
                                    });

                                $("<span>")
                                    .text(secretPlaceholder)
                                    .addClass('text')
                                    .appendTo($item)
                                $("<span>")
                                    .addClass('tooltiptext')
                                    .text('Copy placeholder')
                                    .appendTo($item)
                            });
                        });
                    }
                });

            $("<ul>")
                .addClass("cms-secretselector-list")
                .appendTo($panel);
        }
    }
}

class JQueryExtentArguments {
    method: string;
    opts: any;

    constructor(args: IArguments) {
        if (args.length == 0)
            return;

        this.method = typeof args[0] === 'string' ? args[0] : null;
        if (this.method)
            this.opts = args.length > 1 ? args[1] : null;
        else
            this.opts = args[0];
    }
};

//jquery plugin wrapper
(function (w, $) {
    //no jQuery around
    if (!$) return false;

    function jQueryPlugin(opts, defaults, createInstance) {
        //defaults
        //var defaults: UI.NavbarOptions = new UI.NavbarOptions([{ name: 'root', path: '' }]);

        //extend the defaults!
        var opts = $.extend({}, defaults, opts)

        return this.each(function () {
            var o = opts;
            var obj = $(this);
            createInstance(obj, o);
        });
    }

    $.fn.extend({
        navbar: function (opts) {
            return jQueryPlugin.apply(this,
                [
                    opts,
                    new UI.NavbarOptions([{ name: 'root', path: '' }]),
                    function (obj, o) { return new UI.Navbar(obj, o) }
                ]);
        },
        pinlist: function (opts) {
            return jQueryPlugin.apply(this, [
                opts,
                new UI.PinListOptions([]),
                function (obj, o) { return new UI.PinList(obj, o); }
            ]);
        },
        navtree: function (opts) {
            return jQueryPlugin.apply(this,
                [
                    opts,
                    new UI.NavtreeOptions([], []),
                    function (obj, o) { return new UI.Navtree(obj, o) }
                ]);
        },
        toolbar: function (opts) {
            return jQueryPlugin.apply(this,
                [
                    opts,
                    new UI.ToolbarOptions(),
                    function (obj, o) { return new UI.Toolbar(obj, o) }
                ]);
        },
        contentNodes: function (opts) {
            return jQueryPlugin.apply(this,
                [
                    opts,
                    new UI.ContentNodeOptions(),
                    function (obj, o) { return new UI.ContentNodes(obj, o) }
                ]);
        },
        modalDialog: function (opts) {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.ModalDialogOptions(),
                    function (obj, o) { return new UI.ModalDialog(obj, o, args.method) }
                ]);
        },
        propertyGrid: function (opts) {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.PropertyGridOptions(),
                    function (obj, o) { return new UI.PropertyGrid(obj, o, args.method) }
                ]);
        },
        form: function () {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.FormOptions(),
                    function (obj, o) { return new UI.Form(obj, o, args.method) }
                ]);
        },
        lazyNavTree: function () {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.LazyNavTreeOptions(),
                    function (obj, o) { return new UI.LazyNavTree(obj, o, args.method) }
                ]);
        },
        hourglass: function () {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.HourglassOptions(),
                    function (obj, o) { return new UI.Hourglass(obj, o, args.method) }
                ]);
        },
        descriptionPanel: function () {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.DescriptionPanelOptions(),
                    function (obj, o) { return new UI.DescriptionPanel(obj, o, args.method) }
                ]);
        },
        secretSelectorPanel: function () {
            var args = new JQueryExtentArguments(arguments);
            return jQueryPlugin.apply(this,
                [
                    args.opts,
                    new UI.SecretSelectorPanelOptions(),
                    function (obj, o) { return new UI.SecretSelectorPanel(obj, o, args.method) }
                ]);
        }
    });

})(window, jQuery);
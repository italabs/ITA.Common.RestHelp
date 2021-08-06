
$(document).ready(function () {
    $("#inputOperation").treeview({ collapsed: true });        
    $("#outputOperation").treeview({ collapsed: true });
    $(".folder").each(function () { $(this).click(); });
});
//===============================================================================================================
// System  : Color Syntax Highlighter
// File    : Highlight.js
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/20/2022
// Note    : Copyright 2006-2022, Eric Woodruff, All rights reserved
//
// This contains the script to expand and collapse the regions in the syntax highlighted code.
//
//===============================================================================================================

// Expand/collapse a region
function HighlightExpandCollapse(showId, hideId)
{
    var showSpan = document.getElementById(showId), hideSpan = document.getElementById(hideId);

    showSpan.style.display = "inline";
    hideSpan.style.display = "none";
}

$(document).ready(() => {
    $('#inputFile').on("change", updateSize);
    $('#startWorkitem').on("click", startWorkItem);   
   
  
})


// The workerSrc property shall be specified.
pdfjsLib.GlobalWorkerOptions.workerSrc = "js/pdf.js/pdf.worker.js"

var pdfDoc = null,
    pageNum = 1,
    pageRendering = false,
    pageNumPending = null,
    scale = 0.8,
    canvas = document.getElementById("pdf-canvas"),
    ctx = canvas.getContext("2d");

/**
 * Get page info from document, resize canvas accordingly, and render page.
 * @param num Page number.
 */
let renderPage = (num) =>{
    pageRendering = true;
    // Using promise to fetch the page
    pdfDoc.getPage(num).then(function (page) {
        var viewport = page.getViewport({ scale: scale });
        canvas.height = viewport.height;
        canvas.width = viewport.width;

        // Render PDF page into canvas context
        var renderContext = {
            canvasContext: ctx,
            viewport: viewport,
        };
        var renderTask = page.render(renderContext);

        // Wait for rendering to finish
        renderTask.promise.then(function () {
            pageRendering = false;
            if (pageNumPending !== null) {
                // New page rendering is pending
                renderPage(pageNumPending);
                pageNumPending = null;
            }
        });
    });

    // Update page counters
    document.getElementById("page_num").textContent = num;
}

/**
 * If another page rendering in progress, waits until the rendering is
 * finised. Otherwise, executes rendering immediately.
 */
let queueRenderPage =(num) =>{
    if (pageRendering) {
        pageNumPending = num;
    } else {
        renderPage(num);
    }
}

/**
 * Displays previous page.
 */
function onPrevPage() {
    if (pageNum <= 1) {
        return;
    }
    pageNum--;
    queueRenderPage(pageNum);
}
document.getElementById("prev").addEventListener("click", onPrevPage);

/**
 * Displays next page.
 */
let onNextPage = () => {
    if (pageNum >= pdfDoc.numPages) {
        return;
    }
    pageNum++;
    queueRenderPage(pageNum);
}
document.getElementById("next").addEventListener("click", onNextPage);






function writeLog(text) {
    $('#outputlog').append(text);
    var elem = document.getElementById('outputlog');
    elem.scrollTop = elem.scrollHeight;
}
const spinner = $('#spinner');
const Spin = function (){
    this.start = () => {
        spinner.addClass("show");
    }
    this.stop = () => {
        spinner.removeClass("show");
    }
}

var connection;
var connectionId;

let updateSize = async () => {

    var inputFileId = $('#inputFile')[0];
    if (inputFileId.files.length === 0) { alert('Please select an input file'); return; }
    let nBytes = 0,
        oFiles = inputFileId.files,
        nFiles = oFiles.length,
        file = inputFileId.files[0];
    for (let nFileId = 0; nFileId < nFiles; nFileId++) {
        nBytes += oFiles[nFileId].size;
    }
    let sOutput = nBytes + " bytes";
    // optional code for multiples approximation
    const aMultiples = ["KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB"];
    for (nMultiple = 0, nApprox = nBytes / 1024; nApprox > 1; nApprox /= 1024, nMultiple++) {
        sOutput = nApprox.toFixed(3) + " " + aMultiples[nMultiple] + " (" + nBytes + " bytes)";
    }
    // end of optional code
    $('#fileNum').html(nFiles);
    $('#fileSize').html(sOutput);
    await startConnection(async () => {
        try {
            const spin = new Spin();
            var formData = new FormData();
            formData.append('inputFile', file);
            formData.append('data', JSON.stringify({
                browserConnectionId: connectionId
            }));
            writeLog('\nUploading input file...');
            spin.start();
            let response = await fetch('/api/forge/designautomation/upload', { method: `POST`, mode: `cors`, body: formData });
            if (response.ok) {
                var body = await response.json();
                spin.stop();
                writeLog('\nUpload to OSS has completed...');
                writeLog('\n');
                writeLog(JSON.stringify(body, null, 2));                
            }
        } catch (e) {
            console.error(e);
        }});
}

let startConnection = async (onReady) => {
    const spin = new Spin();
    if (connection && connection.connectionState) { if (onReady) onReady(); return; }
    connection = new signalR
        .HubConnectionBuilder()
        .withUrl("/api/signalr/designautomation")
        .build();
    await connection.start();
    let id = await connection.invoke('getConnectionId');
    connectionId = id;
    if (onReady) {        
        onReady();
    }
    
    connection.on("downloadResult", (url) => {       
        writeLog('<a href="' + url + '">Download result file here</a>');        
        pdfjsLib.getDocument(url).promise.then(function (pdfDoc_) {
            pdfDoc = pdfDoc_;
            document.getElementById("page_count").textContent = pdfDoc.numPages;
            // Initial/first page rendering
            renderPage(pageNum);
        });

    });

    connection.on("onComplete", (message) => {
        spin.stop();
        writeLog(message);
    });
}

let startWorkItem = async () => {
    try {
        const spin = new Spin();
        spin.start();
        let response = await fetch('/api/forge/designautomation/workitems', { method: `POST`, mode: `cors` });
        if (response.ok) {
            var body = await response.json();           
            writeLog('\nWorkitem started: ' + body.workItemId);
        }
    } catch (e) {
        console.error(e);
    }   
}




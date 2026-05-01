<script lang="ts">
  import { invoke } from "@tauri-apps/api/core";
  import { Window } from "@tauri-apps/api/window";
  import { slide } from "svelte/transition";
  import { cubicOut } from "svelte/easing";
  import { 
    File, 
    FloppyDisk, 
    Printer, 
    MagnifyingGlassPlus, 
    MagnifyingGlassMinus,
    HandHand,
    Cursor,
    CaretLeft,
    CaretRight,
    SidebarSimple,
    BookmarkSimple,
    MagnifyingGlass,
    IdentificationBadge,
    DotsThreeVertical,
    CheckCircle,
    X,
    Minus,
    Square,
    Copy
  } from "phosphor-svelte";

  import { open, save } from "@tauri-apps/plugin-dialog";
  import { documentStore, type PDFDocument } from "$lib/stores/documentStore";
  import CombineModal from "$lib/components/CombineModal.svelte";

  const appWindow = Window.getCurrent();

  let leftPanelOpen = $state(true);
  let rightPanelOpen = $state(true);
  let combineModalOpen = $state(false);
  let isStampMode = $state(false);
  let isMaximized = $state(false);
  let selectedPages = $state<Set<number>>(new Set());
  let currentTool = $state<"Select" | "Hand" | "Stamp">("Select");
  let isPanning = $state(false);
  let panStart = { x: 0, y: 0, scrollLeft: 0, scrollTop: 0 };

  // Derive current document state from store
  let activeDoc = $derived($documentStore.documents.find(d => d.id === $documentStore.activeId));
  let documents = $derived($documentStore.documents);

  const setTool = (tool: "Select" | "Hand" | "Stamp") => {
    currentTool = tool;
    isStampMode = (tool === "Stamp");
  };

  function handleMouseDown(e: MouseEvent) {
    if (currentTool === "Hand" && activeDoc) {
      isPanning = true;
      const container = e.currentTarget as HTMLElement;
      panStart = {
        x: e.clientX,
        y: e.clientY,
        scrollLeft: container.scrollLeft,
        scrollTop: container.scrollTop
      };
    }
  }

  function handleMouseMove(e: MouseEvent) {
    if (isPanning && currentTool === "Hand") {
      const container = e.currentTarget as HTMLElement;
      const dx = e.clientX - panStart.x;
      const dy = e.clientY - panStart.y;
      container.scrollLeft = panStart.scrollLeft - dx;
      container.scrollTop = panStart.scrollTop - dy;
    }
  }

  function handleMouseUp() {
    isPanning = false;
  }

  $effect(() => {
    // Reset selection when switching docs
    if ($documentStore.activeId) {
      selectedPages = new Set();
    }
  });

  function togglePageSelection(index: number, multi: boolean, range: boolean) {
    if (!activeDoc) return;
    if (range && selectedPages.size > 0) {
      const lastSelected = Array.from(selectedPages).pop()!;
      const start = Math.min(lastSelected, index);
      const end = Math.max(lastSelected, index);
      for (let i = start; i <= end; i++) selectedPages.add(i);
    } else if (multi) {
      if (selectedPages.has(index)) selectedPages.delete(index);
      else selectedPages.add(index);
    } else {
      selectedPages = new Set([index]);
    }
  }

  async function deleteSelectedPages() {
    if (!activeDoc || selectedPages.size === 0) return;
    
    // In a real editor, we'd update the total pages and re-render.
    // For this prototype, we'll mark them as deleted and simulate the removal.
    const remainingThumbnails = activeDoc.thumbnails.filter((_, i) => !selectedPages.has(i));
    documentStore.updateDocument(activeDoc.id, { 
      thumbnails: remainingThumbnails,
      totalPages: remainingThumbnails.length,
      currentPage: Math.min(activeDoc.currentPage, remainingThumbnails.length) || 1
    });
    
    selectedPages = new Set();
    renderPage(activeDoc.id, (activeDoc.currentPage || 1) - 1);
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === "Delete" && selectedPages.size > 0) {
      deleteSelectedPages();
    }
  }

  const toggleLeft = () => leftPanelOpen = !leftPanelOpen;
  const toggleRight = () => rightPanelOpen = !rightPanelOpen;

  async function handleCanvasClick(e: MouseEvent) {
    if (!isStampMode || !activeDoc) return;
    
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    
    const scale = 100 / activeDoc.zoom;
    const nativeX = Math.round(x * scale);
    const nativeY = Math.round(y * scale);
    
    try {
      const stampPath = "C:\\Users\\areol\\OneDrive - Wizard株式会社\\優香共有用\\gnosticlibrary\\Production\\PickleBall\\アプリ\\Photon X Pro\\Professional_Hanko.png";
      const result = await invoke<string>("apply_stamp", {
        pageIndex: activeDoc.currentPage - 1,
        stampPath,
        x: nativeX,
        y: nativeY,
        pageW: 800,
        pageH: 1131,
        stampW: 100,
        stampH: 100
      });
      documentStore.updateDocument(activeDoc.id, { pdfImage: result });
      isStampMode = false;
    } catch (e) {
      console.error(e);
    }
  }

  const minimize = () => appWindow.minimize();
  const toggleMaximize = async () => {
    await appWindow.toggleMaximize();
    isMaximized = await appWindow.isMaximized();
  };
  const close = () => appWindow.close();

  async function openFile(specificPath?: string) {
    try {
      let path = specificPath;
      if (!path) {
        path = await open({
          multiple: false,
          filters: [{ name: 'PDF', extensions: ['pdf'] }]
        }) as string;
      }

      if (!path) return;

      const totalPages = await invoke<number>("open_pdf", { path });
      const name = path.split(/[\\/]/).pop() || "Untitled.pdf";
      
      const newDoc: PDFDocument = {
        id: Math.random().toString(36).substr(2, 9),
        name,
        path,
        totalPages,
        currentPage: 1,
        zoom: 100,
        pdfImage: null,
        thumbnails: new Array(totalPages).fill("")
      };

      documentStore.addDocument(newDoc);
      await renderPage(newDoc.id, 0);
      
      // Generate thumbnails in background
      for (let i = 0; i < totalPages; i++) {
        const thumbUrl = await invoke<string>("render_page", { 
          index: i, 
          width: 150, 
          height: 212 
        });
        documentStore.updateDocument(newDoc.id, {
          thumbnails: newDoc.thumbnails.map((t, idx) => idx === i ? thumbUrl : t)
        });
      }
    } catch (e) {
      console.error(e);
    }
  }

  async function renderPage(docId: string, index: number) {
    const doc = $documentStore.documents.find(d => d.id === docId);
    if (!doc) return;

    try {
      const baseWidth = 800;
      const baseHeight = 1131;
      const w = Math.round(baseWidth * (doc.zoom / 100));
      const h = Math.round(baseHeight * (doc.zoom / 100));
      
      const dataUrl = await invoke<string>("render_page", { 
        index, 
        width: w, 
        height: h 
      });
      documentStore.updateDocument(docId, { pdfImage: dataUrl });
    } catch (e) {
      console.error(e);
    }
  }

  function nextPage() {
    if (activeDoc && activeDoc.currentPage < activeDoc.totalPages) {
      const newPage = activeDoc.currentPage + 1;
      documentStore.updateDocument(activeDoc.id, { currentPage: newPage });
      renderPage(activeDoc.id, newPage - 1);
    }
  }

  function prevPage() {
    if (activeDoc && activeDoc.currentPage > 1) {
      const newPage = activeDoc.currentPage - 1;
      documentStore.updateDocument(activeDoc.id, { currentPage: newPage });
      renderPage(activeDoc.id, newPage - 1);
    }
  }

  function setZoom(val: number) {
    if (activeDoc) {
      documentStore.updateDocument(activeDoc.id, { zoom: val });
      renderPage(activeDoc.id, activeDoc.currentPage - 1);
    }
  }

  function handlePageInput(e: KeyboardEvent) {
    if (e.key === "Enter" && activeDoc) {
      const val = parseInt((e.target as HTMLInputElement).value);
      if (val >= 1 && val <= activeDoc.totalPages) {
        documentStore.updateDocument(activeDoc.id, { currentPage: val });
        renderPage(activeDoc.id, val - 1);
      }
    }
  }

  async function saveAs() {
    if (!activeDoc) return;
    try {
      const path = await save({
        defaultPath: activeDoc.name,
        filters: [{ name: 'PDF', extensions: ['pdf'] }]
      });
      
      if (path) {
        // Use SaveWithPageDeletion as a generic save for now
        const success = await invoke<boolean>("save_pdf", { 
          outputPath: path,
          deletedPages: [] 
        });
        if (success) {
          console.log("Saved to", path);
        }
      }
    } catch (e) {
      console.error(e);
    }
  }
</script>

<div class="h-screen w-screen flex flex-col overflow-hidden select-none text-text-primary bg-workspace" onkeydown={handleKeyDown} tabindex="-1">
  <!-- Title Bar / Tabs -->
  <div class="h-8 adobe-toolbar flex items-center px-2 justify-between drag-region shrink-0">
    <div class="flex items-center space-x-4 h-full no-drag">
      <div class="text-xxs font-black tracking-[0.2em] text-accent-blue ml-2 italic">PHOTON X PRO</div>
      <div class="flex space-x-0.5 h-full items-end overflow-x-auto custom-scrollbar no-drag">
        {#each documents as doc}
          <button 
            onclick={() => documentStore.setActive(doc.id)}
            class="px-4 py-1.5 {doc.id === $documentStore.activeId ? 'bg-panel border-t-2 border-accent-blue' : 'hover:bg-white/5 text-text-muted'} text-[10px] flex items-center space-x-3 rounded-t-sm shadow-inner transition-all whitespace-nowrap"
          >
            <span class="font-medium">{doc.name}</span>
            <button 
              onclick={(e) => { e.stopPropagation(); documentStore.removeDocument(doc.id); }}
              class="hover:bg-white/10 rounded-full p-0.5"
            >
              <X size={10} />
            </button>
          </button>
        {/each}
      </div>
    </div>
    <div class="flex items-center h-full no-drag">
      <button onclick={minimize} class="h-full px-4 hover:bg-white/10 transition-colors flex items-center"><Minus size={14} /></button>
      <button onclick={toggleMaximize} class="h-full px-4 hover:bg-white/10 transition-colors flex items-center">
        {#if isMaximized}
          <Copy size={12} />
        {:else}
          <Square size={12} />
        {/if}
      </button>
      <button onclick={close} class="h-full px-4 hover:bg-red-600 transition-colors flex items-center group">
        <X size={14} class="group-hover:scale-110 transition-transform" />
      </button>
    </div>
  </div>

  <!-- Menu Bar -->
  <div class="h-8 adobe-toolbar flex items-center px-4 space-x-4 text-xs">
    {#each ['File', 'Edit', 'View', 'Document', 'Tools', 'Window', 'Help'] as menu}
      <div class="adobe-button px-2 py-1 rounded cursor-default">{menu}</div>
    {/each}
  </div>

  <!-- Quick Toolbar -->
  <div class="h-10 adobe-toolbar flex items-center px-4 justify-between">
    <div class="flex items-center space-x-2">
      <div class="flex space-x-1">
        <button class="adobe-button p-1.5 rounded" onclick={() => openFile()}><File size={18} /></button>
        <button class="adobe-button p-1.5 rounded"><FloppyDisk size={18} /></button>
        <button class="adobe-button p-1.5 rounded"><Printer size={18} /></button>
      </div>
      <div class="w-px h-6 bg-panel-border mx-2"></div>
      <button 
        class="adobe-button px-3 py-1.5 rounded text-xs flex items-center space-x-2 bg-white/5 hover:bg-white/10 transition-all border border-transparent hover:border-accent-blue/30"
        onclick={() => combineModalOpen = true}
      >
        <Plus size={16} class="text-accent-blue" />
        <span>Combine Files</span>
      </button>
      <div class="w-px h-6 bg-panel-border mx-2"></div>
      <div class="flex items-center space-x-2">
        <button class="adobe-button p-1.5 rounded" onclick={() => setZoom((activeDoc?.zoom || 100) - 25)}><MagnifyingGlassMinus size={18} /></button>
        <select 
          class="bg-panel border border-panel-border text-xs px-2 py-0.5 rounded outline-none"
          onchange={(e) => setZoom(parseInt((e.target as HTMLSelectElement).value))}
          value={activeDoc?.zoom || 100}
        >
          <option value="50">50%</option>
          <option value="75">75%</option>
          <option value="100">100%</option>
          <option value="125">125%</option>
          <option value="150">150%</option>
          <option value="200">200%</option>
        </select>
        <button class="adobe-button p-1.5 rounded" onclick={() => setZoom((activeDoc?.zoom || 100) + 25)}><MagnifyingGlassPlus size={18} /></button>
        <div class="w-px h-6 bg-panel-border mx-2"></div>
      <div class="flex space-x-1">
        <button 
          class="adobe-button p-1.5 rounded {currentTool === 'Hand' ? 'bg-white/10 text-accent-blue shadow-inner' : ''}" 
          onclick={() => setTool('Hand')}
          title="Hand Tool (H)"
        >
          <HandHand size={18} weight={currentTool === 'Hand' ? "fill" : "regular"} />
        </button>
        <button 
          class="adobe-button p-1.5 rounded {currentTool === 'Select' ? 'bg-white/10 text-accent-blue shadow-inner' : ''}" 
          onclick={() => setTool('Select')}
          title="Select Tool (V)"
        >
          <Cursor size={18} weight={currentTool === 'Select' ? "fill" : "regular"} />
        </button>
      </div>
    </div>
    </div>
    
    <div class="flex items-center space-x-4">
      <button 
        class="flex items-center space-x-2 px-3 py-1 rounded text-xs font-bold transition-all {currentTool === 'Stamp' ? 'bg-accent-blue text-white shadow-lg shadow-blue-500/30' : 'bg-white/5 hover:bg-white/10 text-text-primary'}"
        onclick={() => setTool('Stamp')}
      >
        <IdentificationBadge size={16} weight={currentTool === 'Stamp' ? "fill" : "regular"} />
        <span>Stamp Tool</span>
      </button>
      <div class="w-px h-6 bg-panel-border"></div>
      <button class="bg-accent-blue hover:bg-blue-600 text-white px-4 py-1 rounded text-xs font-bold transition-colors">
        Quick Sign
      </button>
      <div class="w-px h-6 bg-panel-border"></div>
      <button class="adobe-button p-1.5 rounded" onclick={toggleRight}><DotsThreeVertical size={18} /></button>
    </div>
  </div>

  <!-- Main Content Area -->
  <div class="flex-1 flex overflow-hidden">
    <!-- Left Navigation Bar -->
    <div class="w-10 adobe-panel border-r flex flex-col items-center py-4 space-y-4">
      <button class="adobe-button p-2 rounded text-accent-blue" onclick={toggleLeft}><SidebarSimple size={20} /></button>
      <button class="adobe-button p-2 rounded text-text-muted"><BookmarkSimple size={20} /></button>
      <button class="adobe-button p-2 rounded text-text-muted"><MagnifyingGlass size={20} /></button>
      <button class="adobe-button p-2 rounded text-text-muted"><IdentificationBadge size={20} /></button>
    </div>

    <!-- Left Expandable Panel -->
    {#if leftPanelOpen}
      <div 
        class="w-60 adobe-panel border-r flex flex-col overflow-hidden shrink-0"
        transition:slide={{ axis: 'x', duration: 250, easing: cubicOut }}
      >
        <div class="p-3 border-b border-panel-border flex justify-between items-center bg-white/5">
          <span class="text-xxs font-bold tracking-widest text-text-muted uppercase">Page Thumbnails</span>
          <button onclick={toggleLeft}><X size={14} class="text-text-muted hover:text-white" /></button>
        </div>
        <div class="flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar">
          {#if activeDoc}
            {#each activeDoc.thumbnails as thumb, i}
              <button 
                class="flex flex-col items-center space-y-1 w-full group outline-none"
                onclick={(e) => { 
                  togglePageSelection(i, e.ctrlKey || e.metaKey, e.shiftKey);
                  documentStore.updateDocument(activeDoc.id, { currentPage: i + 1 }); 
                  renderPage(activeDoc.id, i); 
                }}
              >
                <div class="w-32 h-44 bg-white shadow-md border group-hover:border-accent-blue group-focus:border-accent-blue transition-all duration-200 relative overflow-hidden flex items-center justify-center {selectedPages.has(i) ? 'ring-2 ring-accent-blue ring-offset-2 ring-offset-panel' : ''} {activeDoc.currentPage === i + 1 ? 'border-accent-blue/50' : ''}">
                  {#if thumb}
                    <img src={thumb} alt="Page {i + 1}" class="w-full h-full object-contain" />
                  {:else}
                    <div class="absolute inset-0 bg-gray-200 animate-pulse flex items-center justify-center">
                      <span class="text-gray-400 text-[10px] font-bold">Rendering</span>
                    </div>
                  {/if}
                  {#if selectedPages.has(i)}
                    <div class="absolute top-1 right-1 bg-accent-blue text-white rounded-full p-0.5 shadow-sm">
                      <Check size={10} weight="bold" />
                    </div>
                  {/if}
                </div>
                <span class="text-xxs {selectedPages.has(i) ? 'text-accent-blue font-bold' : 'text-text-muted'}">Page {i + 1}</span>
              </button>
            {/each}
          {:else}
            <div class="h-full flex items-center justify-center text-text-muted text-[10px] italic p-10 text-center">
              No document open
            </div>
          {/if}
        </div>
      </div>
    {/if}

    <div 
      class="flex-1 overflow-auto flex justify-center p-8 relative scroll-smooth bg-workspace/50 {currentTool === 'Hand' ? (isPanning ? 'cursor-grabbing' : 'cursor-grab') : ''}"
      onmousedown={handleMouseDown}
      onmousemove={handleMouseMove}
      onmouseup={handleMouseUp}
      onmouseleave={handleMouseUp}
    >
      <div class="flex flex-col space-y-8 h-fit">
        <!-- PDF Page Container -->
        {#if activeDoc}
          <button 
            class="bg-white shadow-2xl relative overflow-hidden transition-all duration-300 ease-out text-left cursor-default {currentTool === 'Stamp' ? 'cursor-crosshair ring-2 ring-accent-blue/30' : ''} {currentTool === 'Hand' ? 'pointer-events-none' : ''}" 
            style="width: {800 * (activeDoc.zoom / 100)}px; height: {1131 * (activeDoc.zoom / 100)}px;"
            onclick={handleCanvasClick}
          >
             {#if activeDoc.pdfImage}
                <img src={activeDoc.pdfImage} alt="PDF Page" class="w-full h-full object-contain" />
             {:else}
                <div class="absolute inset-0 flex items-center justify-center bg-white">
                   <div class="w-12 h-12 border-4 border-accent-blue border-t-transparent rounded-full animate-spin"></div>
                </div>
             {/if}
             
             <!-- Page Shadow Edge -->
             <div class="absolute inset-0 pointer-events-none border border-black/5"></div>
          </button>
        {:else}
          <!-- Dashboard Placeholder -->
          <div 
            class="bg-white shadow-2xl relative overflow-hidden flex flex-col" 
            style="width: 800px; height: 1131px;"
            in:fade={{ duration: 600, delay: 200 }}
          >
              <div class="absolute inset-0 flex items-center justify-center p-20 text-center">
                 <div class="space-y-6" in:slide={{ duration: 800, easing: cubicOut }}>
                   <div class="flex justify-center">
                      <div class="w-16 h-16 bg-accent-blue/10 rounded-2xl flex items-center justify-center shadow-inner">
                        <FilePdf size={40} class="text-accent-blue" weight="fill" />
                      </div>
                   </div>
                   <div class="space-y-2">
                     <h1 class="text-5xl font-black tracking-tighter text-gray-900 italic">PHOTON X PRO</h1>
                     <p class="text-[11px] font-bold tracking-[0.4em] text-accent-blue uppercase opacity-70">Native PDF Management Station</p>
                   </div>
                   <p class="text-gray-500 text-xs max-w-sm mx-auto leading-relaxed">
                     Experience the raw power of SIMD-optimized rendering and the precision of 
                     native desktop architecture. Designed for those who demand excellence.
                   </p>
                   <div class="flex flex-col items-center space-y-3 pt-6">
                     <button 
                      onclick={() => openFile()}
                      class="group relative px-10 py-3 bg-gray-900 text-white rounded-full font-bold text-sm transition-all hover:bg-accent-blue hover:scale-105 shadow-xl hover:shadow-blue-500/40"
                     >
                      <span class="relative z-10 flex items-center space-x-2">
                        <Plus size={18} weight="bold" />
                        <span>Get Started</span>
                      </span>
                     </button>
                     <p class="text-[10px] text-gray-400">or drag and drop a file here</p>
                   </div>
                   <div class="h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent w-full my-10"></div>
                   <div class="grid grid-cols-3 gap-8 opacity-40">
                      <div class="flex flex-col items-center space-y-2">
                        <div class="w-10 h-10 border border-gray-300 rounded-full flex items-center justify-center"><HandHand size={20} /></div>
                        <span class="text-[9px] font-bold uppercase tracking-widest">Fluid Pan</span>
                      </div>
                      <div class="flex flex-col items-center space-y-2">
                        <div class="w-10 h-10 border border-gray-300 rounded-full flex items-center justify-center"><BoundingBox size={20} /></div>
                        <span class="text-[9px] font-bold uppercase tracking-widest">Precise</span>
                      </div>
                      <div class="flex flex-col items-center space-y-2">
                        <div class="w-10 h-10 border border-gray-300 rounded-full flex items-center justify-center"><Lightning size={20} /></div>
                        <span class="text-[9px] font-bold uppercase tracking-widest">SIMD Speed</span>
                      </div>
                   </div>
                 </div>
              </div>
          </div>
        {/if}
      </div>

      <!-- Floating HUD Stats -->
      <div class="absolute top-4 left-4 bg-black/60 backdrop-blur-md p-4 rounded-lg border border-white/10 w-48 shadow-2xl">
         <div class="text-[9px] font-bold text-accent-blue mb-2 tracking-tighter uppercase">Engine Telemetry</div>
         <div class="space-y-1">
            <div class="flex justify-between text-[10px]"><span class="text-text-muted">Renderer:</span><span class="text-green-400 font-mono">WinRT/AVX</span></div>
            <div class="flex justify-between text-[10px]"><span class="text-text-muted">GPU Accel:</span><span class="text-text-primary">Enabled</span></div>
            <div class="flex justify-between text-[10px]"><span class="text-text-muted">Draw Calls:</span><span class="text-text-primary">12ms</span></div>
         </div>
      </div>
    </div>

    <!-- Right Task Pane -->
    {#if rightPanelOpen}
      <div class="w-64 adobe-panel border-l flex flex-col overflow-hidden">
        <div class="h-10 bg-white/5 border-b border-panel-border flex">
          <button 
            class="flex-1 text-[10px] font-bold tracking-wider uppercase {activeTab === 'Tools' ? 'text-accent-blue border-b-2 border-accent-blue' : 'text-text-muted'}"
            onclick={() => activeTab = 'Tools'}
          >Tools</button>
          <button 
            class="flex-1 text-[10px] font-bold tracking-wider uppercase {activeTab === 'Comment' ? 'text-accent-blue border-b-2 border-accent-blue' : 'text-text-muted'}"
            onclick={() => activeTab = 'Comment'}
          >Comment</button>
        </div>
        <div class="flex-1 overflow-y-auto p-4">
          <div class="space-y-2">
            {#each ['Pages', 'Content Editing', 'Forms', 'Protection', 'Sign & Certify'] as tool}
              <div class="flex items-center justify-between p-2 hover:bg-white/5 rounded cursor-pointer group">
                <span class="text-xs text-text-primary group-hover:text-accent-blue">{tool}</span>
                <CaretRight size={12} class="text-text-muted" />
              </div>
            {/each}
          </div>
        </div>
      </div>
    {/if}
  </div>

  <!-- Status Bar -->
  <div class="h-7 adobe-toolbar border-t border-panel-border flex items-center px-4 justify-between text-[10px]">
    <div class="flex items-center space-x-4">
      <div class="flex items-center space-x-1">
        <input 
          type="text" 
          value={activeDoc?.currentPage || 1} 
          onkeydown={handlePageInput}
          class="w-8 bg-black/30 border border-panel-border text-center rounded outline-none" 
        />
        <span class="text-text-muted">/ {activeDoc?.totalPages || 0}</span>
      </div>
      <div class="w-px h-3 bg-panel-border"></div>
      <div class="flex items-center space-x-2">
        <button class="hover:text-accent-blue" onclick={prevPage}><CaretLeft size={14} /></button>
        <button class="hover:text-accent-blue" onclick={nextPage}><CaretRight size={14} /></button>
      </div>
    </div>
    
    <div class="flex items-center space-x-6">
      <div class="flex items-center space-x-2">
        <span class="text-text-muted">{activeDoc?.zoom || 100}%</span>
        <div class="w-24 h-1 bg-panel-border rounded-full relative group cursor-pointer">
           <input 
            type="range" min="50" max="200" step="10" 
            value={activeDoc?.zoom || 100} 
            oninput={(e) => setZoom(parseInt((e.target as HTMLInputElement).value))}
            class="absolute inset-0 opacity-0 cursor-pointer z-10" 
           />
           <div class="absolute h-full bg-accent-blue rounded-full" style="width: {((activeDoc?.zoom || 100) - 50) / 1.5}%"></div>
           <div class="absolute w-3 h-3 bg-white rounded-full shadow-sm border border-gray-400 -top-1" style="left: {((activeDoc?.zoom || 100) - 50) / 1.5}%"></div>
        </div>
      </div>
      <div class="w-px h-3 bg-panel-border"></div>
      <div class="flex items-center space-x-2 text-green-500">
        <CheckCircle size={12} weight="fill" />
        <span class="font-bold tracking-tighter">ENGINE READY (v1.0.0)</span>
      </div>
    </div>
  </div>
</div>

<CombineModal bind:isOpen={combineModalOpen} onComplete={(path) => openFile(path)} />

<style>
  :global(.drag-region) {
    -webkit-app-region: drag;
  }
  :global(.no-drag) {
    -webkit-app-region: no-drag;
  }
</style>

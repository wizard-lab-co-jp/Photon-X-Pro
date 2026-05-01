<script lang="ts">
  import { X, List, Plus, Trash, FilePdf } from "phosphor-svelte";
  import { invoke } from "@tauri-apps/api/core";
  import { fade, slide } from "svelte/transition";

  let { isOpen = $bindable(), onComplete } = $props();

  let files = $state<string[]>([
    "C:\\Users\\areol\\Documents\\Invoice_April.pdf",
    "C:\\Users\\areol\\Documents\\Report_Q1.pdf"
  ]);

  let isDragging = $state(false);

  function addFile() {
    files.push(`C:\\Users\\areol\\Documents\\New_Document_${files.length + 1}.pdf`);
  }

  function handleDrop(e: DragEvent) {
    e.preventDefault();
    isDragging = false;
    
    if (e.dataTransfer?.files) {
      const newFiles = Array.from(e.dataTransfer.files)
        .filter(f => f.name.toLowerCase().endsWith('.pdf'))
        // Note: In a real Tauri app, getting the full path from File object 
        // requires specific configuration or using the tauri drag-and-drop event.
        // For this demo, we'll use the file name as a placeholder or assume 
        // the user's environment allows path access.
        .map(f => (f as any).path || f.name); 
      
      files = [...files, ...newFiles];
    }
  }

  function handleDragOver(e: DragEvent) {
    e.preventDefault();
    isDragging = true;
  }

  function handleDragLeave() {
    isDragging = false;
  }

  function removeFile(index: number) {
    files = files.filter((_, i) => i !== index);
  }

  let draggedIndex = $state<number | null>(null);

  function handleItemDragStart(index: number) {
    draggedIndex = index;
  }

  function handleItemDrop(targetIndex: number) {
    if (draggedIndex === null) return;
    const items = [...files];
    const [draggedItem] = items.splice(draggedIndex, 1);
    items.splice(targetIndex, 0, draggedItem);
    files = items;
    draggedIndex = null;
  }

  async function handleCombine() {
    try {
      const output = "C:\\Users\\areol\\Documents\\Merged_Output.pdf";
      const success = await invoke<boolean>("merge_pdfs", { paths: files, output });
      if (success) {
        onComplete(output);
        isOpen = false;
      }
    } catch (e) {
      console.error(e);
    }
  }
</script>

{#if isOpen}
  <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm" transition:fade>
    <div class="w-full max-w-lg bg-panel border border-panel-border rounded-lg shadow-2xl overflow-hidden flex flex-col max-h-[80vh]">
      <!-- Header -->
      <div class="p-4 border-b border-panel-border flex justify-between items-center bg-white/5">
        <div class="flex items-center space-x-3">
          <FilePdf size={24} class="text-accent-blue" weight="fill" />
          <h2 class="text-sm font-bold tracking-tight uppercase">Combine Files</h2>
        </div>
        <button onclick={() => isOpen = false} class="adobe-button p-1 rounded">
          <X size={18} />
        </button>
      </div>

      <!-- File List -->
      <div 
        class="flex-1 overflow-y-auto p-4 space-y-2 custom-scrollbar transition-colors duration-200 {isDragging ? 'bg-accent-blue/10 border-2 border-dashed border-accent-blue/50 mx-2 my-2 rounded-lg' : ''}"
        ondragover={handleDragOver}
        ondragleave={handleDragLeave}
        ondrop={handleDrop}
      >
        {#each files as file, i}
          <div 
            class="group flex items-center justify-between p-3 bg-white/5 border border-white/5 rounded-md hover:border-accent-blue/50 transition-all {draggedIndex === i ? 'opacity-40' : ''}"
            draggable="true"
            ondragstart={() => handleItemDragStart(i)}
            ondragover={(e) => e.preventDefault()}
            ondrop={() => handleItemDrop(i)}
            transition:slide
          >
            <div class="flex items-center space-x-3 truncate">
              <List size={16} class="text-text-muted cursor-grab active:cursor-grabbing" />
              <div class="flex flex-col truncate pointer-events-none">
                <span class="text-xs font-medium truncate">{file.split(/[\\/]/).pop()}</span>
                <span class="text-[9px] text-text-muted truncate">{file}</span>
              </div>
            </div>
            <button onclick={() => removeFile(i)} class="text-text-muted hover:text-red-500 p-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <Trash size={16} />
            </button>
          </div>
        {/each}

        <button 
          onclick={addFile}
          class="w-full p-3 border-2 border-dashed border-panel-border rounded-md text-text-muted hover:text-white hover:border-accent-blue transition-all flex items-center justify-center space-x-2 bg-transparent"
        >
          <Plus size={16} />
          <span class="text-xs">Add Files...</span>
        </button>
      </div>

      <!-- Footer -->
      <div class="p-4 border-t border-panel-border bg-white/5 flex justify-end space-x-3">
        <button 
          onclick={() => isOpen = false}
          class="px-4 py-2 text-xs font-bold hover:bg-white/10 rounded transition-colors"
        >Cancel</button>
        <button 
          onclick={handleCombine}
          class="px-6 py-2 bg-accent-blue hover:bg-blue-600 text-white text-xs font-bold rounded shadow-lg shadow-blue-500/20 transition-all"
        >Combine</button>
      </div>
    </div>
  </div>
{/if}

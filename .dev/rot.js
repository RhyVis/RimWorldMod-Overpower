const fs = require("fs");
const path = require("path");

// Get the directory where the script is located
const scriptDir = __dirname;

console.log(`Start processing directory: ${scriptDir}`);
console.log("=".repeat(50));

// Read all files in the directory
fs.readdir(scriptDir, (err, files) => {
  if (err) {
    console.error("Failed to read directory:", err);
    return;
  }

  // Filter files ending with '画板 1' (ignore file extension)
  const targetFiles = files.filter((file) => {
    const nameWithoutExt = path.parse(file).name;
    return nameWithoutExt.endsWith("画板 1");
  });

  if (targetFiles.length === 0) {
    console.log('No files ending with "画板 1" found');
    return;
  }

  console.log(`Found ${targetFiles.length} files to process:`);
  targetFiles.forEach((file) => console.log(`  - ${file}`));
  console.log("");

  let processedCount = 0;
  let totalOperations = targetFiles.length * 4; // Each file needs 3 copies + 1 deletion

  // Process each file
  targetFiles.forEach((originalFile) => {
    const filePath = path.join(scriptDir, originalFile);
    const parsedPath = path.parse(originalFile);
    const baseName = parsedPath.name.replace("画板 1", "");
    const extension = parsedPath.ext;

    // Define new file names
    const newFileNames = ["east", "south", "north"];

    console.log(`Processing file: ${originalFile}`);

    // Copy file to three new files
    newFileNames.forEach((direction) => {
      const newFileName = `${baseName}${direction}${extension}`;
      const newFilePath = path.join(scriptDir, newFileName);

      fs.copyFile(filePath, newFilePath, (err) => {
        if (err) {
          console.error(`  ❌ Failed to copy to ${newFileName}:`, err.message);
        } else {
          console.log(`  ✅ Successfully copied to: ${newFileName}`);
        }

        processedCount++;
        checkCompletion();
      });
    });

    // Delete original file
    fs.unlink(filePath, (err) => {
      if (err) {
        console.error(`  ❌ Failed to delete ${originalFile}:`, err.message);
      } else {
        console.log(
          `  🗑️  Successfully deleted original file: ${originalFile}`
        );
      }

      processedCount++;
      checkCompletion();
    });

    console.log("");
  });

  // Check if all operations are completed
  function checkCompletion() {
    if (processedCount === totalOperations) {
      console.log("=".repeat(50));
      console.log("All operations completed!");
      console.log(`Processed ${targetFiles.length} files`);
      console.log(`Total operations executed: ${totalOperations}`);
    }
  }
});

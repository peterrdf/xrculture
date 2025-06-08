import sys
import os
import time
import shutil
import pymeshlab

time.sleep(2)  # wait for file system to be ready

def copy_all_files(src_dir, dst_dir):
    for root, dirs, files in os.walk(src_dir):
        for file in files:
            src_file = os.path.join(root, file)
            # Compute the relative path and destination file path
            rel_path = os.path.relpath(src_file, src_dir)
            dst_file = os.path.join(dst_dir, rel_path)
            os.makedirs(os.path.dirname(dst_file), exist_ok=True)
            shutil.copy2(src_file, dst_file)  # Overwrites if exists

# Create a new MeshSet
ms = pymeshlab.MeshSet()

ms.load_new_mesh(str(sys.argv[1]))

# Apply Quadric Edge Collapse Decimation with texture preservation
ms.meshing_decimation_quadric_edge_collapse_with_texture()

# Save the optimized mesh
ms.save_current_mesh(str(sys.argv[2]))

time.sleep(2)  # wait for file system to be ready

print("Mesh simplification completed!")

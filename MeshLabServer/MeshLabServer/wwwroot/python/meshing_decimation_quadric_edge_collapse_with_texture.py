import sys
import pymeshlab

# Create a new MeshSet
ms = pymeshlab.MeshSet()

ms.load_new_mesh(sys.argv[1])

# Apply Quadric Edge Collapse Decimation with texture preservation
ms.meshing_decimation_quadric_edge_collapse_with_texture()

# Save the optimized mesh
ms.save_current_mesh(sys.argv[2])

print("Mesh simplification completed!")

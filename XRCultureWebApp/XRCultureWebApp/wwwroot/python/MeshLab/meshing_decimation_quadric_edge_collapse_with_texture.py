import sys
import pymeshlab

# Create a new MeshSet
ms = pymeshlab.MeshSet()

ms.load_new_mesh(sys.argv[1])
#print(ms.current_mesh().vertex_number())
#print(ms.current_mesh().face_number())

# Compute geometric measures
#out_dict = ms.get_geometric_measures()

# Extract specific measures
#avg_edge_length = out_dict['avg_edge_length']
#total_edge_length = out_dict['total_edge_length']
#mesh_volume = out_dict['mesh_volume']
#surface_area = out_dict['surface_area']

# Print results
#print(f"Average Edge Length: {avg_edge_length}")
#print(f"Total Edge Length: {total_edge_length}")
#print(f"Mesh Volume: {mesh_volume}")
#print(f"Surface Area: {surface_area}")

# Apply Quadric Edge Collapse Decimation with texture preservation
ms.meshing_decimation_quadric_edge_collapse_with_texture()
#ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum = ms.current_mesh().face_number() // 2, preservenormal = True)

# Save the optimized mesh
ms.save_current_mesh(sys.argv[2])

print("Mesh simplification completed!")

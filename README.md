# OBJ to Minecraft NBT Converter

A tool to convert `.obj` 3D model files into Minecraft `.nbt` files, designed for use with the **Create mod's Schematicannon**. This allows you to import voxelized 3D models into Minecraft using your chosen block types.

---

## Features

- Converts `.obj` files to Minecraft `.nbt` schematics.
- Splits large models into multiple chunk-sized `.nbt` files.
- Custom block type selection.
- Optional interior flood-fill.

---

## Preparing Models in Blender

To ensure compatibility and proper voxelization, follow these steps in Blender:

1. Import or create your 3D model.
2. Add a **Remesh Modifier**:
   - Set **Mode** to `Blocks`.
   - Apply the modifier once satisfied.
3. Export the model as a **Wavefront (.obj)** file.

> ⚠️ If using flood-fill (`--floodfill`), make sure the object is watertight and the center point lies inside the mesh.

---

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/TillDiebelt/obj-to-minecraft-nbt.git
   cd obj-to-minecraft-nbt
   ```

2. Build with .NET:

   ```bash
    dotnet build
   ```

## Usage

   ```bash
   dotnet run -- [options] <input.obj>
   ```

## Example

   ```bash
   dotnet run -- model.obj -o output -b minecraft:oak_planks -c 512 -f
   ```

This will:

- Read model.obj
- Output chunked .nbt files with base name output
- Use minecraft:oak_planks as the block material
- Split output into ~512-block chunks (slightly padded to avoid overlaps)
- Fill the inside of the model with blocks

## Options

| Option         | Alias | Description                                                                                                    |
| -------------- | ----- | -------------------------------------------------------------------------------------------------------------- |
| `--output`     | `-o`  | Output base path. Default is input filename without extension.                                                 |
| `--chunk-size` | `-c`  | Split output into multiple `.nbt` files of approx. this size per axis. Slightly padded for overlap prevention. |
| `--block-name` | `-b`  | Block ID to use (e.g., `minecraft:stone`, `minecraft:glass`).                                                  |
| `--floodfill`  | `-f`  | Fill the interior of the model. ⚠️ Ensure the mesh is closed and center is inside the object.                  |

## Notes

- This tool does not voxelize geometry — use Blender's Remesh modifier in Blocks mode for that.
- The --chunk-size option controls output chunk splitting, not model resolution.
- Output files will be named like output_chunk_0_0_0.nbt, etc.
- Compatible with Create mod’s Schematicannon and other NBT structure tools.

## Third-Party Libraries

This project uses the following third-party libraries:

- fNbt
    Licensed under the BSD 3-Clause License.
    See THIRD_PARTY_LICENSES.md for full license text.

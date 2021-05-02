import os
import argparse
import modules.extractor as extractor
import modules.generator as generator

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Flatbuffer Extension')
    parser.add_argument('--dir', default='.')
    parser.add_argument('--output', default='output')
    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    result = extractor.load(args.dir)
    for namespace, dataSet in result.items():
        os.makedirs(f'{args.output}/go/{namespace}', exist_ok=True)
        for data in dataSet:
            with open(f"{args.output}/go/{namespace}/{data['name']['upper']}.go", 'w', encoding='utf8') as f:
                result = generator.go(namespace, data['name'], data['params'])
                f.write(result)

        os.makedirs(f'{args.output}/cs/{namespace}', exist_ok=True)
        for data in dataSet:
            with open(f"{args.output}/cs/{namespace}/{data['name']['upper']}.cs", 'w', encoding='utf8') as f:
                result = generator.cs(namespace, data['name'], data['params'])
                f.write(result)
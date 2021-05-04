import os
import argparse
import modules.extractor as extractor
import modules.generator as generator

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Flatbuffer Extension')
    parser.add_argument('--root', default='..')
    parser.add_argument('--dir', default='.')
    parser.add_argument('--output', default='output')
    parser.add_argument('--namespace', default='Protocol.{namespace}')
    args = parser.parse_args()

    os.makedirs(args.output, exist_ok=True)

    result = extractor.load(args.dir)
    for namespace, dataSet in result.items():
        namespace_f = os.path.join(args.root, *namespace.split('.')[:-1])
        package = namespace.split('.')[-1]
        namespace = args.namespace.replace('{namespace}', package)
        path = namespace.replace('.', '/')

        os.makedirs(f'{args.output}/go/{path}', exist_ok=True)
        for data in dataSet:
            with open(f"{args.output}/go/{path}/{data['name']}.go", 'w', encoding='utf8') as f:
                result = generator.go(namespace_f, package, data['name'], data['params'])
                f.write(result)

        os.makedirs(f'{args.output}/cs/{path}', exist_ok=True)
        for data in dataSet:
            with open(f"{args.output}/cs/{path}/{data['name']}.cs", 'w', encoding='utf8') as f:
                result = generator.cs(namespace, data['name'], data['params'])
                f.write(result)
